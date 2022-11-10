using MongoDB.Driver;
using RestSharp;
using System.Text;
using MongoDB.Bson;
using System.Dynamic;
using Common.Models.x42Blockcore;
using Newtonsoft.Json;
using Common.Enums;
using Common.Models.XDocuments.DNS;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Serialization;
using MongoDB.Bson.Serialization;
using xServerWorker.Services;
using MongoDB.Driver.Builders;
using Common.Models.XDocuments.Zones;
using Common.Models.XServer;
using Common.Services;
using System.Linq.Expressions;
using Common.Models.DApps.Models;
using File = Common.Models.DApps.Models.File;

namespace xServerWorker.BackgroundServices
{
    public class BlockProcessingWorker : BackgroundService
    {
        private readonly ILogger<BlockProcessingWorker> _logger;
        private static int latestBlockProcessed = 1910000;

#if DEBUG
        private readonly RestClient _restClient = new RestClient("http://localhost:42220/api/");
#else
        private readonly RestClient _restClient = new RestClient("http://x42core:42220/api/");
#endif




        private readonly MongoClient _client;
        private readonly IMongoDatabase _db;
        private readonly PowerDnsRestClient _powerDnsRestClient;
        private readonly IDappProvisioner _dappProvisioner;

        private static string _powerDnsHost = "";
        private static string _powerDnsApiKey = "";
        private static string _xServerHost = "http://x42server:4242/";
        private static string _feeAddress = "";


        private static List<SimpleBlockModel> _blockHashes = new List<SimpleBlockModel>();

        public BlockProcessingWorker(ILogger<BlockProcessingWorker> logger, PowerDnsRestClient powerDnsRestClient, IDappProvisioner dappProvisioner)
        {
            var mongoConnectionString = Environment.GetEnvironmentVariable("MONGOCONNECTIONSTRING");

            _logger = logger;

            _powerDnsHost = Environment.GetEnvironmentVariable("POWERDNSHOST") ?? "";
            _powerDnsApiKey = Environment.GetEnvironmentVariable("PDNS_API_KEY") ?? "";



#if DEBUG
            _powerDnsHost = "https://poweradmin.xserver.network";
            _powerDnsApiKey = "cmp4V1Z0MnprRVRMbE10";
            _xServerHost = "http://127.0.0.1:4242/";
            _client = new MongoClient($"mongodb://localhost:27017");


#else
            _client = new MongoClient(mongoConnectionString);
            //_xServerHost = Environment.GetEnvironmentVariable("XSERVER_BACKEND") ?? "";

#endif



            _db = _client.GetDatabase("xServerDb");

            var options = new CreateIndexOptions() { Unique = false };
            var field = new StringFieldDefinition<BsonDocument>("keyAddress");
            var indexDefinition = new IndexKeysDefinitionBuilder<BsonDocument>().Ascending(field);

            var indexModel = new CreateIndexModel<BsonDocument>(indexDefinition, options);
            _db.GetCollection<BsonDocument>("DnsZones").Indexes.CreateOne(indexModel);
            _powerDnsRestClient = powerDnsRestClient;
            _dappProvisioner = dappProvisioner;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var blockCount = await GetBlockCount();
                _feeAddress = await GetMyFeeAddress();

                var dictionaryCollection = _db.GetCollection<BsonDocument>("Dictionary");


                var filter = Builders<BsonDocument>.Filter.Eq("_id", "latestBlock");
                var document = dictionaryCollection.Find(filter).FirstOrDefault();

                if (document != null)
                {
                    var id = document["value"].ToString();

                    if (id != null)
                    {
                        latestBlockProcessed = int.Parse(id);

                    }

                }
                else
                {
                    dynamic lastBlockEntry = new ExpandoObject();
                    lastBlockEntry._id = "latestBlock";
                    lastBlockEntry.value = latestBlockProcessed;


                    var jsonstring = JsonConvert.SerializeObject(lastBlockEntry);
                    var updateDocument = BsonSerializer.Deserialize<BsonDocument>(jsonstring);

                    dictionaryCollection.InsertOne(updateDocument);
                    document = updateDocument;

                }


                _logger.LogInformation($"Latest block processed : {latestBlockProcessed}");
                _logger.LogInformation($"Latest block height : {blockCount}");

                var taskList = new List<Task<string>>();
                var consinueTaskList = new List<Task>();


                _logger.LogInformation("Processsing Blocks...");




                while (!stoppingToken.IsCancellationRequested)
                {
                    if (latestBlockProcessed < blockCount && _feeAddress != "")
                    {
                        await ProcessBlock(stoppingToken);

                        if (document != null)
                        {
                            var builder = Builders<BsonDocument>.Filter;

                            filter = builder.Eq("_id", "latestBlock");
                            var update = Builders<BsonDocument>.Update.Set("value", latestBlockProcessed);
                            dictionaryCollection.UpdateOne(filter, update);

                        }

                    }
                    else
                    {


                        blockCount = await GetBlockCount();

                    }
                }
            }
            catch (Exception e)
            {

                throw;
            }
        }

        private async Task ProcessBlock(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Processing Block : {latestBlockProcessed}");

            var blockHash = await GetBlockHash(latestBlockProcessed);

            var block = await GetBlock(blockHash);

            var paidToMyFeeAddress = false;
            double amount = 0;
            foreach (var transaction in block.Transactions)
            {
                foreach (var vout in transaction.Vout)
                {

                    if (vout.ScriptPubKey.Addresses != null && vout.ScriptPubKey.Addresses.Contains(_feeAddress))
                    {

                        paidToMyFeeAddress = true;
                        amount = vout.Value;

                    }
                }
            }

            paidToMyFeeAddress =  block.Transactions.Where(l => l.Vout.Any(x => x.ScriptPubKey.Addresses != null && x.ScriptPubKey.Addresses.Contains(_feeAddress))).Any();

            if (paidToMyFeeAddress)
            {
                var vouts = block.Transactions.SelectMany(l => l.Vout).Where(l => l.ScriptPubKey.Addresses != null && l.ScriptPubKey.Addresses.Contains(_feeAddress)).ToList();

                amount = vouts.Sum(l => l.Value);


            }

            var vOutList = block.Transactions.SelectMany(l => l.Vout);

 

            foreach (var vOut in vOutList)
            {

                if (vOut.ScriptPubKey.Asm.Contains("OP_RETURN"))
                {
                    var asmHex = vOut.ScriptPubKey.Asm.Replace("OP_RETURN", "").Replace(" ", "");
                    if (asmHex.Length != 66)
                    {
                        _logger.LogInformation($"Instruction found with hex {asmHex} at block {latestBlockProcessed}");
                        byte[] data = FromHex(asmHex);

                        string instructionHash = Encoding.ASCII.GetString(data);
                        _logger.LogInformation($"Instruction hash {instructionHash}");

                        _logger.LogInformation("------------------------------------------------------------------------");
                        _logger.LogInformation($"Check Mongo Pending Transactions, if it exists, process the instruction");
                        _logger.LogInformation("------------------------------------------------------------------------");

                        var xDocumentHashCollection = _db.GetCollection<BsonDocument>("XDocumentHashReference");

                        

                        var filter = Builders<BsonDocument>.Filter.Eq("hash", instructionHash);
                        var document = xDocumentHashCollection.Find(filter).FirstOrDefault();

                        var xDocumentPendingCollection = _db.GetCollection<BsonDocument>("xDocumentPending");

                        if (document != null)
                        {
                            var id = document["_id"].ToString();


                            filter = Builders<BsonDocument>.Filter.Eq("_id", id);
                            document = xDocumentPendingCollection.Find(filter).FirstOrDefault();

                            try
                            {
                                

                                    await ProcessInstruction(document, xDocumentPendingCollection, amount,paidToMyFeeAddress, block.Height);

                                 

                            }
                            catch (Exception e)
                            {

                                _logger.LogError(e.Message);
                            }


                        }

                    }
                }
            }
            latestBlockProcessed++;
        }

        private async Task ProcessInstruction(BsonDocument document, IMongoCollection<BsonDocument> xDocumentPendingCollection, double amountPaid, bool paidToMyFeeAddress, int blockHeight)
        {
            if (document != null)
            {
                int instructionType = Convert.ToInt32(document["instructionType"]);
                var dynamicObject = JsonConvert.DeserializeObject<dynamic>(document.ToString());
                string _keyAddress = dynamicObject["keyAddress"];

                var data = dynamicObject["data"];
 

                switch (instructionType)
                {
                    case (int)InstructionTypeEnum.NewDnsZone:

                        await NewDnsZone(document, xDocumentPendingCollection, dynamicObject, blockHeight, amountPaid, paidToMyFeeAddress);

                        break;

                    case (int)InstructionTypeEnum.UpdateDnsZone:

                        await UpdateDnsZone(document, xDocumentPendingCollection, dynamicObject, blockHeight);

                        break;

                    case (int)InstructionTypeEnum.NewApp:

                        await NewDapp(document, xDocumentPendingCollection, dynamicObject, blockHeight, amountPaid, paidToMyFeeAddress);

                        break;
                    case (int)InstructionTypeEnum.RegisterNewNameserver:

                        if (dynamicObject != null)
                        {

                            string nameServer = (string)dynamicObject["data"]["ns"];
                            string ipAddress = (string)dynamicObject["data"]["ip"];

                        }

                        break;


                    default:

                        throw new Exception($"Instruction Type ({instructionType}) not found");

                }

            }
        }

        public async Task UpdateDnsZone(BsonDocument document, IMongoCollection<BsonDocument> xDocumentPendingCollection, dynamic dynamicObject, int blockHeight)
        {
            if (dynamicObject != null)
            {


                _logger.LogInformation($"Update Zone Instruction Found");
                _logger.LogInformation($"Updating DNS Zone");

                string zone = dynamicObject["data"]["zone"];
                var rrSetsData = dynamicObject["data"]["rrsets"];

                var rrSets = JsonConvert.DeserializeObject<List<RrSet>>(rrSetsData.ToString());

                var zoneUpdate = false;

                try
                {
                    await _powerDnsRestClient.UpdateDNSZone(dynamicObject, blockHeight, rrSets);
                    zoneUpdate = true;

                }
                catch (Exception e)
                {
                    _logger.LogInformation($"Updating DNS zone '{zone}'failed");
                    _logger.LogInformation(e.Message);

                }

                if (zoneUpdate)
                {

                    MoveDocument(document, xDocumentPendingCollection);

                }

            }
        }

        private async Task NewDnsZone(BsonDocument document, IMongoCollection<BsonDocument> xDocumentPendingCollection, dynamic dynamicObject, int blockHeight, double amountPaid, bool paidToMyFeeAddress)
        {
            if (dynamicObject != null)
            {
                string zone = (string)dynamicObject["data"]["zone"];

                _logger.LogInformation($"New Zone Instruction Found");
                _logger.LogInformation($"Creating DNS Zone");

                var zoneCreated = false;

                try
                {
 
                    await _powerDnsRestClient.CreateDNSZone(dynamicObject, blockHeight);
                    zoneCreated = true;

                }
                catch (Exception e)
                {
                    _logger.LogInformation($"Creating DNS zone '{zone}'failed");
                    _logger.LogInformation(e.Message);

                }

                if (zoneCreated)
                {

                    MoveDocument(document, xDocumentPendingCollection);

                }

            }
        }
        private async Task NewDapp(BsonDocument document, IMongoCollection<BsonDocument> xDocumentPendingCollection, dynamic dynamicObject, int blockHeight, double amountPaid, bool paidToMyFeeAddress)
        {
            if (dynamicObject != null)
            {
                string appname = (string)dynamicObject["data"]["appname"];
                string domain = (string)dynamicObject["data"]["domain"];
                string email = (string)dynamicObject["data"]["email"];

                _logger.LogInformation($"New DApp Instruction Found");
                _logger.LogInformation($"Creating {appname} App");

                var zoneCreated = false;

                try
                {
                    DappDeploymentModel deploymentModel = new DappDeploymentModel()
                    {

                        Args = new Dictionary<string, string>
               {
                    { "appname", appname },
                    { "domain", domain },
                    { "email", email },
                    { "MYSQL_PASSWORD", "!Coco1nut" },
                    { "MYSQL_ROOT_PASSWORD", "!Coco1nut" }
              }
                    };

                    DappDefinitionModel dappDefinitionModel = new DappDefinitionModel()
                    {
                        appName = "wordpress",
                        deploymentVersion = 1,
                        envVars = new Dictionary<string, string>
                {
                    { "TimeZone", "America/New_York" },
                    { "OLS_VERSION", "1.7.15" },
                    { "PHP_VERSION", "lsphp80" },

                    { "MYSQL_DATABASE", "{appname}" },
                    { "MYSQL_USER", "wordpress" },
                    { "MYSQL_PASSWORD", "{MYSQL_PASSWORD}" },
                    { "MYSQL_ROOT_PASSWORD", "{MYSQL_ROOT_PASSWORD}" }
              },
                        deploymentScriptSet = new DeploymentScriptSet()
                        {
                            deploymentScript = new DeployScript[3]
                             {
                         new DeployScript
                         {
                             seq = 1,
                             preContainer = true,
                             filename = "pre_deploy_site.sh",

                         },
                         new DeployScript
                         {
                             seq = 1,
                             postContainer = true,
                             filename = "post_deploy_site.sh",
                         },
                         new DeployScript
                         {
                             composeScript = true,
                             path = "./{domain}",
                             filename = "docker-compose.yml",
                         }
                             },
                            jsonForms = new JsonForms()
                            {
                                schema = "",
                                uiSchema = ""
                            }
                        },
                        files = new File[]
                        {
                    new File
                    {
                        path= "",
                        filename="pre_deploy_site.sh",
                        content = "IyEvYmluL2Jhc2gKCmlmIFsgJCMgLWx0IDUgXTsgdGhlbgogIGVjaG8gIlVzYWdlOiAkMCBhcHBuYW1lIGRvbWFpbiBlbWFpbCBteS53cC5kYi5wYXNzd29yZCBteS5yb290LmRiLnBhc3N3b3JkIgogIGVjaG8gIlVzYWdlOiAkMCB3b3JkcHJlc3MgbXl3b3JkcHJlc3Mud29yZHByZXNzcHJldmlldy5zaXRlIG15QGVtYWlsLmNvbSBteXNlY2V0d3BwYXNzIG15c3VwZXJzZWNyZXRyb290cGFzcyIKICBleGl0IDEKZmkKCkFQUF9OQU1FPSQxCkRPTUFJTj0kMgpET01BSU5fTE9XRVI9JChlY2hvICIkRE9NQUlOIiB8IHRyICdbOnVwcGVyOl0nICdbOmxvd2VyOl0nIHwgc2VkICdzL1wuLydfJy9nJykKRU1BSUw9JDMKTVlTUUxfUEFTU1dPUkQ9JDQKTVlTUUxfUk9PVF9QQVNTV09SRD0kNQoKbWFpbigpewoKCgllY2hvICJTZXR0aW5nIFVwICR7RE9NQUlOX0xPV0VSfSIKCW1rZGlyICR7RE9NQUlOfQoJc2VkIC1lICdzLyNET01BSU4jLycke0RPTUFJTn0nL2cnIC1lICdzLyNkb21haW4jLycke0RPTUFJTl9MT1dFUn0nL2cnIGRvY2tlci1jb21wb3NlLnltbCA+ICR7RE9NQUlOfS9kb2NrZXItY29tcG9zZS55bWwKCWNwIC1yIGJpbiAke0RPTUFJTn0vCgljZCAke0RPTUFJTn0KCgljYXQgPDxFT0YgPiAuZW52ClRpbWVab25lPUFtZXJpY2EvTmV3X1lvcmsKT0xTX1ZFUlNJT049MS43LjE1ClBIUF9WRVJTSU9OPWxzcGhwODAKTVlTUUxfREFUQUJBU0U9d29yZHByZXNzCk1ZU1FMX1JPT1RfUEFTU1dPUkQ9JHtNWVNRTF9ST09UX1BBU1NXT1JEfQpNWVNRTF9VU0VSPXdvcmRwcmVzcwpNWVNRTF9QQVNTV09SRD0ke01ZU1FMX1BBU1NXT1JEfQpET01BSU49JHtET01BSU59CkVPRgoKCW1rZGlyIGRhdGEKCW1rZGlyIGxvZ3MKCW1rZGlyIGxzd3MKCW1rZGlyIHNpdGVzCn0KCm1haW4K"
                    },

                    new File
                    {
                        path= "",
                        filename="post_deploy_site.sh",
                        content = "IyEvYmluL2Jhc2gKCmlmIFsgJCMgLWx0IDUgXTsgdGhlbgogIGVjaG8gIlVzYWdlOiAkMCBhcHBuYW1lIGRvbWFpbiBlbWFpbCBteS53cC5kYi5wYXNzd29yZCBteS5yb290LmRiLnBhc3N3b3JkIgogIGVjaG8gIlVzYWdlOiAkMCB3b3JkcHJlc3MgbXl3b3JkcHJlc3Mud29yZHByZXNzcHJldmlldy5zaXRlIG15QGVtYWlsLmNvbSBteXNlY2V0d3BwYXNzIG15c3VwZXJzZWNyZXRyb290cGFzcyIKICBleGl0IDEKZmkKCkFQUF9OQU1FPSQxCkRPTUFJTj0kMgpET01BSU5fTE9XRVI9JChlY2hvICIkRE9NQUlOIiB8IHRyICdbOnVwcGVyOl0nICdbOmxvd2VyOl0nIHwgc2VkICdzL1wuLydfJy9nJykKRE9NQUlOX05PRE9UPSQoZWNobyAiJERPTUFJTl9MT1dFUiIgfCBzZWQgJ3MvXF8vL2cnKQpFTUFJTD0kMwpNWVNRTF9QQVNTV09SRD0kNApNWVNRTF9ST09UX1BBU1NXT1JEPSQ1CgptYWluKCl7CgoJY2QgJHtET01BSU59CgkKCgkKCWRvY2tlciBjb250YWluZXIgY3JlYXRlIC0tbmFtZSAgdGVtcF9jb250YWluZXIxIC12ICR7RE9NQUlOX05PRE9UfV9jb250YWluZXI6L3Vzci9sb2NhbC9iaW4gYnVzeWJveAoJZG9ja2VyIGNwIC4vYmluL2NvbnRhaW5lci8uIHRlbXBfY29udGFpbmVyMTovdXNyL2xvY2FsL2JpbgoJZG9ja2VyIHJtIHRlbXBfY29udGFpbmVyMQoJCglkb2NrZXIgcnVuIC0tcm0gLWRpdCAtdiAke0RPTUFJTl9OT0RPVH1fc2l0ZXM6L3Zhci93d3cvdmhvc3RzLyR7RE9NQUlOfS8gYWxwaW5lIGFzaCAtYyAibWtkaXIgL3Zhci93d3cvdmhvc3RzLyR7RE9NQUlOfS8ke0RPTUFJTn0iCglkb2NrZXIgcnVuIC0tcm0gLWRpdCAtdiAke0RPTUFJTl9OT0RPVH1fc2l0ZXM6L3Zhci93d3cvdmhvc3RzLyR7RE9NQUlOfS8gYWxwaW5lIGFzaCAtYyAibWtkaXIgL3Zhci93d3cvdmhvc3RzLyR7RE9NQUlOfS8ke0RPTUFJTn0vaHRtbCIKCWRvY2tlciBydW4gLS1ybSAtZGl0IC12ICR7RE9NQUlOX05PRE9UfV9zaXRlczovdmFyL3d3dy92aG9zdHMvJHtET01BSU59LyBhbHBpbmUgYXNoIC1jICJta2RpciAvdmFyL3d3dy92aG9zdHMvJHtET01BSU59LyR7RE9NQUlOfS9sb2dzIgoJZG9ja2VyIHJ1biAtLXJtIC1kaXQgLXYgJHtET01BSU5fTk9ET1R9X3NpdGVzOi92YXIvd3d3L3Zob3N0cy8ke0RPTUFJTn0vIGFscGluZSBhc2ggLWMgIm1rZGlyIC92YXIvd3d3L3Zob3N0cy8ke0RPTUFJTn0vJHtET01BSU59L2NlcnRzIgoJCgoJZWNobyAiQWRkaW5nIERvbWFpbiAke0RPTUFJTn0iCglzb3VyY2UgLi9iaW4vZG9tYWluLnNoIC1BICR7RE9NQUlOfQoJCgllY2hvICJBZGRpbmcgRGF0YWJhc2UiCgliYXNoIC4vYmluL2RhdGFiYXNlLnNoIC1EICR7RE9NQUlOfQoKCQoJZG9ja2VyIGNvbnRhaW5lciBjcmVhdGUgLS1uYW1lICB0ZW1wX2NvbnRhaW5lcjIgLXYgJHtET01BSU5fTk9ET1R9X3NpdGVzOi92YXIvd3d3L3Zob3N0cyBidXN5Ym94Cglkb2NrZXIgY3Agc2l0ZXMvJHtET01BSU59Ly5kYl9wYXNzIHRlbXBfY29udGFpbmVyMjovdmFyL3d3dy92aG9zdHMvJHtET01BSU59Ly5kYl9wYXNzCglkb2NrZXIgcm0gdGVtcF9jb250YWluZXIyCgoJZWNobyAiSW5zdGFsbGluZyAke0FQUF9OQU1FfSBvbiAke0RPTUFJTn0iCgliYXNoIC4vYmluL2FwcGluc3RhbGwuc2ggLUEgJHtBUFBfTkFNRX0gLUQgJHtET01BSU59CgkKCWVjaG8gIkRvbmUuIgp9CgptYWluCg=="
                    },

                    new File
                    {
                        path= "",
                        filename="docker-compose.yml",
                        content = "dmVyc2lvbjogJzMnCnNlcnZpY2VzOgogIG15c3FsOgogICAgaW1hZ2U6IG1hcmlhZGI6MTAuNS45CiAgICBjb21tYW5kOiAtLW1heF9hbGxvd2VkX3BhY2tldD0yNTZNCiAgICB2b2x1bWVzOgogICAgICAtICJkYXRhOi92YXIvbGliL215c3FsIgogICAgZW52aXJvbm1lbnQ6CiAgICAgIE1ZU1FMX1JPT1RfUEFTU1dPUkQ6ICR7TVlTUUxfUk9PVF9QQVNTV09SRH0KICAgICAgTVlTUUxfREFUQUJBU0U6ICR7TVlTUUxfREFUQUJBU0V9CiAgICAgIE1ZU1FMX1VTRVI6ICR7TVlTUUxfVVNFUn0KICAgICAgTVlTUUxfUEFTU1dPUkQ6ICR7TVlTUUxfUEFTU1dPUkR9CiAgICByZXN0YXJ0OiBhbHdheXMKICAgIG5ldHdvcmtzOgogICAgICAjZG9tYWluIzoKICAgICAgICBhbGlhc2VzOgogICAgICAgICAgLSAjZG9tYWluIwogICAgaGVhbHRoY2hlY2s6CiAgICAgIHRlc3Q6IFsiQ01EIiwgIm15c3FsYWRtaW4iICwicGluZyIsICItaCIsICJsb2NhbGhvc3QiXQogICAgICB0aW1lb3V0OiAyMHMKICAgICAgcmV0cmllczogNQoKICBsaXRlc3BlZWQ6CiAgICBpbWFnZTogbGl0ZXNwZWVkdGVjaC9vcGVubGl0ZXNwZWVkOiR7T0xTX1ZFUlNJT059LSR7UEhQX1ZFUlNJT059CiAgICBlbnZpcm9ubWVudDoKICAgICAgVFo6IEFtZXJpY2EvTmV3X1lvcmsKICAgIGxhYmVsczoKICAgICAgLSAidHJhZWZpay5lbmFibGU9dHJ1ZSIKICAgICAgLSAidHJhZWZpay5kb2NrZXIubmV0d29yaz1wcm94eSIKICAgICAgLSAidHJhZWZpay5odHRwLnJvdXRlcnMuI2RvbWFpbiMucnVsZT1Ib3N0KGAjRE9NQUlOI2ApIgogICAgICAtICJ0cmFlZmlrLmh0dHAucm91dGVycy4jZG9tYWluIy5lbnRyeXBvaW50cz13ZWJzZWN1cmUiCiAgICAgIC0gInRyYWVmaWsuaHR0cC5zZXJ2aWNlcy4jZG9tYWluIy5sb2FkYmFsYW5jZXIuc2VydmVyLnBvcnQ9ODAiCiAgICAgIC0gInRyYWVmaWsuaHR0cC5yb3V0ZXJzLiNkb21haW4jLnRscy5jZXJ0cmVzb2x2ZXI9bXlyZXNvbHZlciIKICAgIHZvbHVtZXM6CiAgICAgICAgLSBsc3dzX2NvbmY6L3Vzci9sb2NhbC9sc3dzL2NvbmYKICAgICAgICAtIGxzd3NfYWRtaW4tY29uZjovdXNyL2xvY2FsL2xzd3MvYWRtaW4vY29uZgogICAgICAgIC0gY29udGFpbmVyOi91c3IvbG9jYWwvYmluCiAgICAgICAgLSBzaXRlczovdmFyL3d3dy92aG9zdHMvCiAgICAgICAgLSBhY21lOi9yb290Ly5hY21lLnNoLwogICAgICAgIC0gbG9nczovdXNyL2xvY2FsL2xzd3MvbG9ncy8KICAgIHJlc3RhcnQ6IGFsd2F5cwogICAgbmV0d29ya3M6CiAgICAgIHByb3h5OgogICAgICAgIGFsaWFzZXM6CiAgICAgICAgICAtIHByb3h5CiAgICAgICNkb21haW4jOgogICAgICAgIGFsaWFzZXM6CiAgICAgICAgICAtICNkb21haW4jCiAgICBkZXBlbmRzX29uOgogICAgICAgICAgICBteXNxbDoKICAgICAgICAgICAgICAgIGNvbmRpdGlvbjogc2VydmljZV9oZWFsdGh5CiAgICAgIApuZXR3b3JrczoKICBwcm94eToKICAgIGV4dGVybmFsOiB0cnVlCiAgICBuYW1lOiBwcm94eQogICNkb21haW4jOgogICAgZXh0ZXJuYWw6IGZhbHNlCiAgICBuYW1lOiAjZG9tYWluIwp2b2x1bWVzOgogIGxzd3NfY29uZjoKICBsc3dzX2FkbWluLWNvbmY6CiAgY29udGFpbmVyOgogIHNpdGVzOgogIGFjbWU6CiAgbG9nczoKICBkYXRhOg=="
                    },

                    new File
                    {
                        path= "bin/",
                        filename="appinstall.sh",
                        content= "IyEvdXNyL2Jpbi9lbnYgYmFzaApBUFBfTkFNRT0nJwpET01BSU49JycKRVBBQ0U9JyAgICAgICAgJwoKZWNob3coKXsKICAgIEZMQUc9JHsxfQogICAgc2hpZnQKICAgIGVjaG8gLWUgIlwwMzNbMW0ke0VQQUNFfSR7RkxBR31cMDMzWzBtJHtAfSIKfQoKaGVscF9tZXNzYWdlKCl7CiAgICBlY2hvIC1lICJcMDMzWzFtT1BUSU9OU1wwMzNbMG0iCiAgICBlY2hvdyAnLUEsIC0tYXBwIFthcHBfbmFtZV0gLUQsIC0tZG9tYWluIFtET01BSU5fTkFNRV0nCiAgICBlY2hvICIke0VQQUNFfSR7RVBBQ0V9RXhhbXBsZTogYXBwaW5zdGFsbC5zaCAtQSB3b3JkcHJlc3MgLUQgZXhhbXBsZS5jb20iCiAgICBlY2hvICIke0VQQUNFfSR7RVBBQ0V9V2lsbCBpbnN0YWxsIFdvcmRQcmVzcyBDTVMgdW5kZXIgdGhlIGV4YW1wbGUuY29tIGRvbWFpbiIKICAgIGVjaG93ICctSCwgLS1oZWxwJwogICAgZWNobyAiJHtFUEFDRX0ke0VQQUNFfURpc3BsYXkgaGVscCBhbmQgZXhpdC4iCiAgICBleGl0IDAKfQoKY2hlY2tfaW5wdXQoKXsKICAgIGlmIFsgLXogIiR7MX0iIF07IHRoZW4KICAgICAgICBoZWxwX21lc3NhZ2UKICAgICAgICBleGl0IDEKICAgIGZpCn0KCmFwcF9kb3dubG9hZCgpewogICAgZG9ja2VyLWNvbXBvc2UgZXhlYyBsaXRlc3BlZWQgc3UgLWMgImFwcGluc3RhbGxjdGwuc2ggLS1hcHAgJHsxfSAtLWRvbWFpbiAkezJ9IgogICAgYmFzaCBiaW4vd2ViYWRtaW4uc2ggLXIKICAgIGV4aXQgMAp9CgptYWluKCl7CiAgICBhcHBfZG93bmxvYWQgJHtBUFBfTkFNRX0gJHtET01BSU59Cn0KCmNoZWNrX2lucHV0ICR7MX0Kd2hpbGUgWyAhIC16ICIkezF9IiBdOyBkbwogICAgY2FzZSAkezF9IGluCiAgICAgICAgLVtoSF0gfCAtaGVscCB8IC0taGVscCkKICAgICAgICAgICAgaGVscF9tZXNzYWdlCiAgICAgICAgICAgIDs7CiAgICAgICAgLVthQV0gfCAtYXBwIHwgLS1hcHApIHNoaWZ0CiAgICAgICAgICAgIGNoZWNrX2lucHV0ICIkezF9IgogICAgICAgICAgICBBUFBfTkFNRT0iJHsxfSIKICAgICAgICAgICAgOzsKICAgICAgICAtW2REXSB8IC1kb21haW4gfCAtLWRvbWFpbikgc2hpZnQKICAgICAgICAgICAgY2hlY2tfaW5wdXQgIiR7MX0iCiAgICAgICAgICAgIERPTUFJTj0iJHsxfSIKICAgICAgICAgICAgOzsgICAgICAgICAgCiAgICAgICAgKikgCiAgICAgICAgICAgIGhlbHBfbWVzc2FnZQogICAgICAgICAgICA7OyAgICAgICAgICAgICAgCiAgICBlc2FjCiAgICBzaGlmdApkb25lCgptYWlu"
                    },

                    new File
                    {
                        path= "bin/",
                        filename="database.sh",
                        content= "IyEvdXNyL2Jpbi9lbnYgYmFzaApzb3VyY2UgLmVudgoKRE9NQUlOPScnClNRTF9EQj0nJwpTUUxfVVNFUj0nJwpTUUxfUEFTUz0nJwpBTlk9IiclJyIKU0VUX09LPTAKRVBBQ0U9JyAgICAgICAgJwoKZWNob3coKXsKICAgIEZMQUc9JHsxfQogICAgc2hpZnQKICAgIGVjaG8gLWUgIlwwMzNbMW0ke0VQQUNFfSR7RkxBR31cMDMzWzBtJHtAfSIKfQoKaGVscF9tZXNzYWdlKCl7CiAgICBlY2hvIC1lICJcMDMzWzFtT1BUSU9OU1wwMzNbMG0iCiAgICBlY2hvdyAnLUQsIC0tZG9tYWluIFtET01BSU5fTkFNRV0nCiAgICBlY2hvICIke0VQQUNFfSR7RVBBQ0V9RXhhbXBsZTogZGF0YWJhc2Uuc2ggLUQgZXhhbXBsZS5jb20iCiAgICBlY2hvICIke0VQQUNFfSR7RVBBQ0V9V2lsbCBhdXRvIGdlbmVyYXRlIERhdGFiYXNlL3VzZXJuYW1lL3Bhc3N3b3JkIGZvciB0aGUgZG9tYWluIgogICAgZWNob3cgJy1ELCAtLWRvbWFpbiBbRE9NQUlOX05BTUVdIC1VLCAtLXVzZXIgW3h4eF0gLVAsIC0tcGFzc3dvcmQgW3h4eF0gLURCLCAtLWRhdGFiYXNlIFt4eHhdJwogICAgZWNobyAiJHtFUEFDRX0ke0VQQUNFfUV4YW1wbGU6IGRhdGFiYXNlLnNoIC1EIGV4YW1wbGUuY29tIC1VIFVTRVJOQU1FIC1QIFBBU1NXT1JEIC1EQiBEQVRBQkFTRU5BTUUiCiAgICBlY2hvICIke0VQQUNFfSR7RVBBQ0V9V2lsbCBjcmVhdGUgRGF0YWJhc2UvdXNlcm5hbWUvcGFzc3dvcmQgYnkgZ2l2ZW4iCiAgICBlY2hvdyAnLUgsIC0taGVscCcKICAgIGVjaG8gIiR7RVBBQ0V9JHtFUEFDRX1EaXNwbGF5IGhlbHAgYW5kIGV4aXQuIgogICAgZXhpdCAwICAgIAp9CgpjaGVja19pbnB1dCgpewogICAgaWYgWyAteiAiJHsxfSIgXTsgdGhlbgogICAgICAgIGhlbHBfbWVzc2FnZQogICAgICAgIGV4aXQgMQogICAgZmkKfQoKc3BlY2lmeV9uYW1lKCl7CiAgICBjaGVja19pbnB1dCAke1NRTF9VU0VSfQogICAgY2hlY2tfaW5wdXQgJHtTUUxfUEFTU30KICAgIGNoZWNrX2lucHV0ICR7U1FMX0RCfQp9CgphdXRvX25hbWUoKXsKICAgIFNRTF9EQj0iJHtUUkFOU05BTUV9IgogICAgU1FMX1VTRVI9IiR7VFJBTlNOQU1FfSIKICAgIFNRTF9QQVNTPSInJHtSQU5ET01fUEFTU30nIgp9CgpnZW5fcGFzcygpewogICAgUkFORE9NX1BBU1M9IiQob3BlbnNzbCByYW5kIC1iYXNlNjQgMTIpIgp9Cgp0cmFuc19uYW1lKCl7CiAgICBUUkFOU05BTUU9JChlY2hvICR7MX0gfCB0ciAtZCAnLiYmLScpCn0KCmRpc3BsYXlfY3JlZGVudGlhbCgpewogICAgaWYgWyAke1NFVF9PS30gPSAwIF07IHRoZW4KICAgICAgICBlY2hvICJEYXRhYmFzZTogJHtTUUxfREJ9IgogICAgICAgIGVjaG8gIlVzZXJuYW1lOiAke1NRTF9VU0VSfSIKICAgICAgICBlY2hvICJQYXNzd29yZDogJChlY2hvICR7U1FMX1BBU1N9IHwgdHIgLWQgIiciKSIKICAgIGZpICAgIAp9CgpzdG9yZV9jcmVkZW50aWFsKCl7CiAgICBpZiBbIC1kICIuL3NpdGVzLyR7MX0iIF07IHRoZW4KICAgICAgICBpZiBbIC1mIC4vc2l0ZXMvJHsxfS8uZGJfcGFzcyBdOyB0aGVuIAogICAgICAgICAgICBtdiAuL3NpdGVzLyR7MX0vLmRiX3Bhc3MgLi9zaXRlcy8kezF9Ly5kYl9wYXNzLmJrCiAgICAgICAgZmkKICAgICAgICBjYXQgPiAiLi9zaXRlcy8kezF9Ly5kYl9wYXNzIiA8PCBFT1QKIkRhdGFiYXNlIjoiJHtTUUxfREJ9IgoiVXNlcm5hbWUiOiIke1NRTF9VU0VSfSIKIlBhc3N3b3JkIjoiJChlY2hvICR7U1FMX1BBU1N9IHwgdHIgLWQgIiciKSIKRU9UCiAgICBlbHNlCiAgICAgICAgZWNobyAiLi9zaXRlcy8kezF9IG5vdCBmb3VuZCwgYWJvcnQgY3JlZGVudGlhbCBzdG9yZSEiCiAgICBmaSAgICAKfQoKY2hlY2tfZGJfYWNjZXNzKCl7CiAgICBkb2NrZXItY29tcG9zZSBleGVjIC1UIG15c3FsIHN1IC1jICJteXNxbCAtdXJvb3QgLXAke01ZU1FMX1JPT1RfUEFTU1dPUkR9IC1lICdzdGF0dXMnIiA+L2Rldi9udWxsIDI+JjEKICAgIGlmIFsgJHs/fSAhPSAwIF07IHRoZW4KICAgICAgICBlY2hvICdbWF0gREIgYWNjZXNzIGZhaWxlZCwgcGxlYXNlIGNoZWNrIScKICAgICAgICBleGl0IDEKICAgIGZpICAgIAp9CgpjaGVja19kYl9leGlzdCgpewogICAgZG9ja2VyLWNvbXBvc2UgZXhlYyAtVCBteXNxbCBzdSAtYyAidGVzdCAtZSAvdmFyL2xpYi9teXNxbC8kezF9IgogICAgaWYgWyAkez99ID0gMCBdOyB0aGVuCiAgICAgICAgZWNobyAiRGF0YWJhc2UgJHsxfSBhbHJlYWR5IGV4aXN0LCBza2lwIERCIGNyZWF0aW9uISIKICAgICAgICBleGl0IDAgICAgCiAgICBmaSAgICAgIAp9CgpkYl9zZXR1cCgpeyAgCiAgICBkb2NrZXItY29tcG9zZSBleGVjIC1UIG15c3FsIHN1IC1jICdteXNxbCAtdXJvb3QgLXAke01ZU1FMX1JPT1RfUEFTU1dPUkR9IFwKICAgIC1lICJDUkVBVEUgREFUQUJBU0UgJyR7U1FMX0RCfSc7IiBcCiAgICAtZSAiR1JBTlQgQUxMIFBSSVZJTEVHRVMgT04gJyR7U1FMX0RCfScuKiBUTyAnJHtTUUxfVVNFUn0nQCcke0FOWX0nIElERU5USUZJRUQgQlkgJyR7U1FMX1BBU1N9JzsiIFwKICAgIC1lICJGTFVTSCBQUklWSUxFR0VTOyInCiAgICBTRVRfT0s9JHs/fQp9CgphdXRvX3NldHVwX21haW4oKXsKICAgIGNoZWNrX2lucHV0ICR7RE9NQUlOfQogICAgZ2VuX3Bhc3MKICAgIHRyYW5zX25hbWUgJHtET01BSU59CiAgICBhdXRvX25hbWUKICAgIGNoZWNrX2RiX2V4aXN0ICR7U1FMX0RCfQogICAgY2hlY2tfZGJfYWNjZXNzCiAgICBkYl9zZXR1cAogICAgZGlzcGxheV9jcmVkZW50aWFsCiAgICBzdG9yZV9jcmVkZW50aWFsICR7RE9NQUlOfQp9CgpzcGVjaWZ5X3NldHVwX21haW4oKXsKICAgIHNwZWNpZnlfbmFtZQogICAgY2hlY2tfZGJfZXhpc3QgJHtTUUxfREJ9CiAgICBjaGVja19kYl9hY2Nlc3MKICAgIGRiX3NldHVwCiAgICBkaXNwbGF5X2NyZWRlbnRpYWwKICAgIHN0b3JlX2NyZWRlbnRpYWwgJHtET01BSU59Cn0KCm1haW4oKXsKICAgIGlmIFsgIiR7U1FMX1VTRVJ9IiAhPSAnJyBdICYmIFsgIiR7U1FMX1BBU1N9IiAhPSAnJyBdICYmIFsgIiR7U1FMX0RCfSIgIT0gJycgXTsgdGhlbgogICAgICAgIHNwZWNpZnlfc2V0dXBfbWFpbgogICAgZWxzZQogICAgICAgIGF1dG9fc2V0dXBfbWFpbgogICAgZmkKfQoKY2hlY2tfaW5wdXQgJHsxfQp3aGlsZSBbICEgLXogIiR7MX0iIF07IGRvCiAgICBjYXNlICR7MX0gaW4KICAgICAgICAtW2hIXSB8IC1oZWxwIHwgLS1oZWxwKQogICAgICAgICAgICBoZWxwX21lc3NhZ2UKICAgICAgICAgICAgOzsKICAgICAgICAtW2REXSB8IC1kb21haW58IC0tZG9tYWluKSBzaGlmdAogICAgICAgICAgICBET01BSU49IiR7MX0iCiAgICAgICAgICAgIDs7CiAgICAgICAgLVt1VV0gfCAtdXNlciB8IC0tdXNlcikgc2hpZnQKICAgICAgICAgICAgU1FMX1VTRVI9IiR7MX0iCiAgICAgICAgICAgIDs7CiAgICAgICAgLVtwUF0gfCAtcGFzc3dvcmR8IC0tcGFzc3dvcmQpIHNoaWZ0CiAgICAgICAgICAgIFNRTF9QQVNTPSInJHsxfSciCiAgICAgICAgICAgIDs7ICAgICAgICAgICAgCiAgICAgICAgLWRiIHwgLURCIHwgLWRhdGFiYXNlfCAtLWRhdGFiYXNlKSBzaGlmdAogICAgICAgICAgICBTUUxfREI9IiR7MX0iCiAgICAgICAgICAgIDs7ICAgICAgICAgICAgCiAgICAgICAgKikgCiAgICAgICAgICAgIGhlbHBfbWVzc2FnZQogICAgICAgICAgICA7OyAgICAgICAgICAgICAgCiAgICBlc2FjCiAgICBzaGlmdApkb25lCm1haW4K"
                    },

                    new File
                    {
                        path= "bin/",
                        filename="domain.sh",
                        content= "IyEvdXNyL2Jpbi9lbnYgYmFzaApDT05UX05BTUU9J2xpdGVzcGVlZCcKRVBBQ0U9JyAgICAgICAgJwoKZWNob3coKXsKICAgIEZMQUc9JHsxfQogICAgc2hpZnQKICAgIGVjaG8gLWUgIlwwMzNbMW0ke0VQQUNFfSR7RkxBR31cMDMzWzBtJHtAfSIKfQoKaGVscF9tZXNzYWdlKCl7CiAgICBlY2hvIC1lICJcMDMzWzFtT1BUSU9OU1wwMzNbMG0iCiAgICBlY2hvdyAiLUEsIC0tYWRkIFtkb21haW5fbmFtZV0iCiAgICBlY2hvICIke0VQQUNFfSR7RVBBQ0V9RXhhbXBsZTogZG9tYWluLnNoIC1BIGV4YW1wbGUuY29tLCB3aWxsIGFkZCB0aGUgZG9tYWluIHRvIExpc3RlbmVyIGFuZCBhdXRvIGNyZWF0ZSBhIG5ldyB2aXJ0dWFsIGhvc3QuIgogICAgZWNob3cgIi1ELCAtLWRlbCBbZG9tYWluX25hbWVdIgogICAgZWNobyAiJHtFUEFDRX0ke0VQQUNFfUV4YW1wbGU6IGRvbWFpbi5zaCAtRCBleGFtcGxlLmNvbSwgd2lsbCBkZWxldGUgdGhlIGRvbWFpbiBmcm9tIExpc3RlbmVyLiIKICAgIGVjaG93ICctSCwgLS1oZWxwJwogICAgZWNobyAiJHtFUEFDRX0ke0VQQUNFfURpc3BsYXkgaGVscCBhbmQgZXhpdC4iICAgIAp9CgpjaGVja19pbnB1dCgpewogICAgaWYgWyAteiAiJHsxfSIgXTsgdGhlbgogICAgICAgIGhlbHBfbWVzc2FnZQogICAgICAgIGV4aXQgMQogICAgZmkKfQoKYWRkX2RvbWFpbigpewogICAgY2hlY2tfaW5wdXQgJHsxfQogICAgZG9ja2VyLWNvbXBvc2UgZXhlYyAke0NPTlRfTkFNRX0gc3UgLXMgL2Jpbi9iYXNoIGxzYWRtIC1jICJjZCAvdXNyL2xvY2FsL2xzd3MvY29uZiAmJiBkb21haW5jdGwuc2ggLS1hZGQgJHsxfSIKICAgIGlmIFsgISAtZCAiLi9zaXRlcy8kezF9IiBdOyB0aGVuIAogICAgICAgIG1rZGlyIC1wIC4vc2l0ZXMvJHsxfS97aHRtbCxsb2dzLGNlcnRzfQogICAgZmkKICAgIGJhc2ggYmluL3dlYmFkbWluLnNoIC1yCn0KCmRlbF9kb21haW4oKXsKICAgIGNoZWNrX2lucHV0ICR7MX0KICAgIGRvY2tlci1jb21wb3NlIGV4ZWMgJHtDT05UX05BTUV9IHN1IC1zIC9iaW4vYmFzaCBsc2FkbSAtYyAiY2QgL3Vzci9sb2NhbC9sc3dzL2NvbmYgJiYgZG9tYWluY3RsLnNoIC0tZGVsICR7MX0iCiAgICBiYXNoIGJpbi93ZWJhZG1pbi5zaCAtcgp9CgpjaGVja19pbnB1dCAkezF9CndoaWxlIFsgISAteiAiJHsxfSIgXTsgZG8KICAgIGNhc2UgJHsxfSBpbgogICAgICAgIC1baEhdIHwgLWhlbHAgfCAtLWhlbHApCiAgICAgICAgICAgIGhlbHBfbWVzc2FnZQogICAgICAgICAgICA7OwogICAgICAgIC1bYUFdIHwgLWFkZCB8IC0tYWRkKSBzaGlmdAogICAgICAgICAgICBhZGRfZG9tYWluICR7MX0KICAgICAgICAgICAgOzsKICAgICAgICAtW2REXSB8IC1kZWwgfCAtLWRlbCB8IC0tZGVsZXRlKSBzaGlmdAogICAgICAgICAgICBkZWxfZG9tYWluICR7MX0KICAgICAgICAgICAgOzsgICAgICAgICAgCiAgICAgICAgKikgCiAgICAgICAgICAgIGhlbHBfbWVzc2FnZQogICAgICAgICAgICA7OyAgICAgICAgICAgICAgCiAgICBlc2FjCiAgICBzaGlmdApkb25lCiAgICAgICAgICA="
                    },

                    new File
                    {
                        path= "bin/",
                        filename="webadmin.sh",
                        content="IyEvdXNyL2Jpbi9lbnYgYmFzaApDT05UX05BTUU9J2xpdGVzcGVlZCcKRVBBQ0U9JyAgICAgICAgJwoKZWNob3coKXsKICAgIEZMQUc9JHsxfQogICAgc2hpZnQKICAgIGVjaG8gLWUgIlwwMzNbMW0ke0VQQUNFfSR7RkxBR31cMDMzWzBtJHtAfSIKfQoKaGVscF9tZXNzYWdlKCl7CiAgICBlY2hvIC1lICJcMDMzWzFtT1BUSU9OU1wwMzNbMG0iCiAgICBlY2hvdyAnW0VudGVyIFlvdXIgUEFTU1dPUkRdJwogICAgZWNobyAiJHtFUEFDRX0ke0VQQUNFfUV4YW1wbGU6IHdlYmFkbWluLnNoIE1ZX1NFQ1VSRV9QQVNTLCB0byB1cGRhdGUgd2ViIGFkbWluIHBhc3N3b3JkIGltbWVkaWF0bHkuIgogICAgZWNob3cgJy1SLCAtLXJlc3RhcnQnCiAgICBlY2hvICIke0VQQUNFfSR7RVBBQ0V9V2lsbCBncmFjZWZ1bGx5IHJlc3RhcnQgTGl0ZVNwZWVkIFdlYiBTZXJ2ZXIuIgogICAgZWNob3cgJy1NLCAtLW1vZC1zZWN1cmUgW2VuYWJsZXxkaXNhYmxlXScKICAgIGVjaG8gIiR7RVBBQ0V9JHtFUEFDRX1FeGFtcGxlOiB3ZWJhZG1pbi5zaCAtTSBlbmFibGUsIHdpbGwgZW5hYmxlIGFuZCBhcHBseSBNb2RfU2VjdXJlIE9XQVNQIHJ1bGVzIG9uIHNlcnZlciIKICAgIGVjaG93ICctVSwgLS11cGdyYWRlJwogICAgZWNobyAiJHtFUEFDRX0ke0VQQUNFfVdpbGwgdXBncmFkZSB3ZWIgc2VydmVyIHRvIGxhdGVzdCBzdGFibGUgdmVyc2lvbiIKICAgIGVjaG93ICctUywgLS1zZXJpYWwgW1lPVVJfU0VSSUFMfFRSSUFMXScKICAgIGVjaG8gIiR7RVBBQ0V9JHtFUEFDRX1XaWxsIGFwcGx5IHlvdXIgc2VyaWFsIG51bWJlciB0byBMaXRlU3BlZWQgV2ViIFNlcnZlci4iCiAgICBlY2hvdyAnLUgsIC0taGVscCcKICAgIGVjaG8gIiR7RVBBQ0V9JHtFUEFDRX1EaXNwbGF5IGhlbHAgYW5kIGV4aXQuIgogICAgZXhpdCAwCn0KCmNoZWNrX2lucHV0KCl7CiAgICBpZiBbIC16ICIkezF9IiBdOyB0aGVuCiAgICAgICAgaGVscF9tZXNzYWdlCiAgICAgICAgZXhpdCAxCiAgICBmaQp9Cgpsc3dzX3Jlc3RhcnQoKXsKICAgIGRvY2tlci1jb21wb3NlIGV4ZWMgLVQgJHtDT05UX05BTUV9IHN1IC1jICcvdXNyL2xvY2FsL2xzd3MvYmluL2xzd3NjdHJsIHJlc3RhcnQgPi9kZXYvbnVsbCcKfQoKYXBwbHlfc2VyaWFsKCl7CiAgICBkb2NrZXItY29tcG9zZSBleGVjICR7Q09OVF9OQU1FfSBzdSAtYyAic2VyaWFsY3RsLnNoIC0tc2VyaWFsICR7MX0iCiAgICBsc3dzX3Jlc3RhcnQKfQoKbW9kX3NlY3VyZSgpewogICAgaWYgWyAiJHsxfSIgPSAnZW5hYmxlJyBdIHx8IFsgIiR7MX0iID0gJ0VuYWJsZScgXTsgdGhlbgogICAgICAgIGRvY2tlci1jb21wb3NlIGV4ZWMgJHtDT05UX05BTUV9IHN1IC1zIC9iaW4vYmFzaCByb290IC1jICJvd2FzcGN0bC5zaCAtLWVuYWJsZSIKICAgICAgICBsc3dzX3Jlc3RhcnQKICAgIGVsaWYgWyAiJHsxfSIgPSAnZGlzYWJsZScgXSB8fCBbICIkezF9IiA9ICdEaXNhYmxlJyBdOyB0aGVuCiAgICAgICAgZG9ja2VyLWNvbXBvc2UgZXhlYyAke0NPTlRfTkFNRX0gc3UgLXMgL2Jpbi9iYXNoIHJvb3QgLWMgIm93YXNwY3RsLnNoIC0tZGlzYWJsZSIKICAgICAgICBsc3dzX3Jlc3RhcnQKICAgIGVsc2UKICAgICAgICBoZWxwX21lc3NhZ2UKICAgIGZpCn0KCmxzX3VwZ3JhZGUoKXsKICAgIGVjaG8gJ1VwZ3JhZGUgd2ViIHNlcnZlciB0byBsYXRlc3Qgc3RhYmxlIHZlcnNpb24uJwogICAgZG9ja2VyLWNvbXBvc2UgZXhlYyAke0NPTlRfTkFNRX0gc3UgLWMgJy91c3IvbG9jYWwvbHN3cy9hZG1pbi9taXNjL2xzdXAuc2ggMj4vZGV2L251bGwnCn0KCnNldF93ZWJfYWRtaW4oKXsKICAgIGVjaG8gJ1VwZGF0ZSB3ZWIgYWRtaW4gcGFzc3dvcmQuJwogICAgbG9jYWwgTFNBRFBBVEg9Jy91c3IvbG9jYWwvbHN3cy9hZG1pbicKICAgIGRvY2tlci1jb21wb3NlIGV4ZWMgJHtDT05UX05BTUV9IHN1IC1zIC9iaW4vYmFzaCBsc2FkbSAtYyBcCiAgICAgICAgJ2lmIFsgLWUgL3Vzci9sb2NhbC9sc3dzL2FkbWluL2ZjZ2ktYmluL2FkbWluX3BocCBdOyB0aGVuIFwKICAgICAgICBlY2hvICJhZG1pbjokKCcke0xTQURQQVRIfScvZmNnaS1iaW4vYWRtaW5fcGhwIC1xICcke0xTQURQQVRIfScvbWlzYy9odHBhc3N3ZC5waHAgJyR7MX0nKSIgPiAnJHtMU0FEUEFUSH0nL2NvbmYvaHRwYXNzd2Q7IFwKICAgICAgICBlbHNlIGVjaG8gImFkbWluOiQoJyR7TFNBRFBBVEh9Jy9mY2dpLWJpbi9hZG1pbl9waHA1IC1xICcke0xTQURQQVRIfScvbWlzYy9odHBhc3N3ZC5waHAgJyR7MX0nKSIgPiAnJHtMU0FEUEFUSH0nL2NvbmYvaHRwYXNzd2Q7IFwKICAgICAgICBmaSc7Cn0KCm1haW4oKXsKICAgIHNldF93ZWJfYWRtaW4gJHsxfQp9CgpjaGVja19pbnB1dCAkezF9CndoaWxlIFsgISAteiAiJHsxfSIgXTsgZG8KICAgIGNhc2UgJHsxfSBpbgogICAgICAgIC1baEhdIHwgLWhlbHAgfCAtLWhlbHApCiAgICAgICAgICAgIGhlbHBfbWVzc2FnZQogICAgICAgICAgICA7OwogICAgICAgIC1bclJdIHwgLXJlc3RhcnQgfCAtLXJlc3RhcnQpCiAgICAgICAgICAgIGxzd3NfcmVzdGFydAogICAgICAgICAgICA7OwogICAgICAgIC1NIHwgLW1vZGUtc2VjdXJlIHwgLS1tb2Qtc2VjdXJlKSBzaGlmdAogICAgICAgICAgICBtb2Rfc2VjdXJlICR7MX0KICAgICAgICAgICAgOzsKICAgICAgICAtbHN1cCB8IC0tbHN1cCB8IC0tdXBncmFkZSB8IC1VKSBzaGlmdAogICAgICAgICAgICBsc191cGdyYWRlCiAgICAgICAgICAgIDs7CiAgICAgICAgLVtzU10gfCAtc2VyaWFsIHwgLS1zZXJpYWwpIHNoaWZ0CiAgICAgICAgICAgIGFwcGx5X3NlcmlhbCAkezF9CiAgICAgICAgICAgIDs7ICAgICAgICAgICAgIAogICAgICAgICopIAogICAgICAgICAgICBtYWluICR7MX0KICAgICAgICAgICAgOzsgICAgICAgICAgICAgIAogICAgZXNhYwogICAgc2hpZnQKZG9uZQ=="
                    },

                    new File
                    {
                        path= "bin/dev/",
                        filename="list-flagged-files.sh",
                        content= "IyEvdXNyL2Jpbi9lbnYgYmFzaApnaXQgbHMtZmlsZXMgLXZ8Z3JlcCAnXlMnCg=="
                    },

                    new File
                    {
                        path= "bin/dev/",
                        filename="no-skip-worktree-conf.sh",
                        content= "IyEvdXNyL2Jpbi9lbnYgYmFzaA0KZmluZCBjb25mIC1tYXhkZXB0aCAxIC10eXBlIGQgXCggISAtbmFtZSAuIFwpIC1leGVjIGJhc2ggLWMgImNkICd7fScgJiYgcHdkICYmIGdpdCBscy1maWxlcyAteiAke3B3ZH0gfCB4YXJncyAtMCBnaXQgdXBkYXRlLWluZGV4IC0tbm8tc2tpcC13b3JrdHJlZSIgXDsNCg=="
                    },

                    new File
                    {
                        path= "bin/dev/",
                        filename="skip-worktree-conf.sh",
                        content= "IyEvdXNyL2Jpbi9lbnYgYmFzaApmaW5kIGNvbmYgLW1heGRlcHRoIDEgLXR5cGUgZCBcKCAhIC1uYW1lIC4gXCkgLWV4ZWMgYmFzaCAtYyAiY2QgJ3t9JyAmJiBwd2QgJiYgZ2l0IGxzLWZpbGVzIC16ICR7cHdkfSB8IHhhcmdzIC0wIGdpdCB1cGRhdGUtaW5kZXggLS1za2lwLXdvcmt0cmVlIiBcOwoK"
                    },

                    new File
                    {
                        path= "bin/container/",
                        filename="appinstallctl.sh",
                        content= "IyEvYmluL2Jhc2gKREVGQVVMVF9WSF9ST09UPScvdmFyL3d3dy92aG9zdHMnClZIX0RPQ19ST09UPScnClZITkFNRT0nJwpBUFBfTkFNRT0nJwpET01BSU49JycKV1dXX1VJRD0nJwpXV1dfR0lEPScnCldQX0NPTlNUX0NPTkY9JycKUFVCX0lQPSQoY3VybCAtcyBodHRwOi8vY2hlY2tpcC5hbWF6b25hd3MuY29tKQpEQl9IT1NUPSdteXNxbCcKUExVR0lOTElTVD0ibGl0ZXNwZWVkLWNhY2hlLnppcCIKVEhFTUU9J3R3ZW50eXR3ZW50eScKRVBBQ0U9JyAgICAgICAgJwoKZWNob3coKXsKICAgIEZMQUc9JHsxfQogICAgc2hpZnQKICAgIGVjaG8gLWUgIlwwMzNbMW0ke0VQQUNFfSR7RkxBR31cMDMzWzBtJHtAfSIKfQoKaGVscF9tZXNzYWdlKCl7CgllY2hvIC1lICJcMDMzWzFtT1BUSU9OU1wwMzNbMG0iCiAgICBlY2hvdyAnLUEsIC1hcHAgW3dvcmRwcmVzc10gLUQsIC0tZG9tYWluIFtET01BSU5fTkFNRV0nCiAgICBlY2hvICIke0VQQUNFfSR7RVBBQ0V9RXhhbXBsZTogYXBwaW5zdGFsbGN0bC5zaCAtLWFwcCB3b3JkcHJlc3MgLS1kb21haW4gZXhhbXBsZS5jb20iCiAgICBlY2hvdyAnLUgsIC0taGVscCcKICAgIGVjaG8gIiR7RVBBQ0V9JHtFUEFDRX1EaXNwbGF5IGhlbHAgYW5kIGV4aXQuIgogICAgZXhpdCAwCn0KCmNoZWNrX2lucHV0KCl7CiAgICBpZiBbIC16ICIkezF9IiBdOyB0aGVuCiAgICAgICAgaGVscF9tZXNzYWdlCiAgICAgICAgZXhpdCAxCiAgICBmaQp9CgpsaW5lY2hhbmdlKCl7CiAgICBMSU5FTlVNPSQoZ3JlcCAtbiAiJHsxfSIgJHsyfSB8IGN1dCAtZDogLWYgMSkKICAgIGlmIFsgLW4gIiR7TElORU5VTX0iIF0gJiYgWyAiJHtMSU5FTlVNfSIgLWVxICIke0xJTkVOVU19IiBdIDI+L2Rldi9udWxsOyB0aGVuCiAgICAgICAgc2VkIC1pICIke0xJTkVOVU19ZCIgJHsyfQogICAgICAgIHNlZCAtaSAiJHtMSU5FTlVNfWkkezN9IiAkezJ9CiAgICBmaSAKfQoKY2tfZWQoKXsKICAgIGlmIFsgISAtZiAvYmluL2VkIF07IHRoZW4KICAgICAgICBlY2hvICJJbnN0YWxsIGVkIHBhY2thZ2UuLiIKICAgICAgICBhcHQtZ2V0IGluc3RhbGwgZWQgLXkgPiAvZGV2L251bGwgMj4mMQogICAgZmkgICAgCn0KCmNrX3VuemlwKCl7CiAgICBpZiBbICEgLWYgL3Vzci9iaW4vdW56aXAgXTsgdGhlbiAKICAgICAgICBlY2hvICJJbnN0YWxsIHVuemlwIHBhY2thZ2UuLiIKICAgICAgICBhcHQtZ2V0IGluc3RhbGwgdW56aXAgLXkgPiAvZGV2L251bGwgMj4mMQogICAgZmkJCQp9CgpnZXRfb3duZXIoKXsKCVdXV19VSUQ9JChzdGF0IC1jICIldSIgJHtERUZBVUxUX1ZIX1JPT1R9KQoJV1dXX0dJRD0kKHN0YXQgLWMgIiVnIiAke0RFRkFVTFRfVkhfUk9PVH0pCglpZiBbICR7V1dXX1VJRH0gLWVxIDAgXSB8fCBbICR7V1dXX0dJRH0gLWVxIDAgXTsgdGhlbgoJCVdXV19VSUQ9MTAwMAoJCVdXV19HSUQ9MTAwMAoJCWVjaG8gIlNldCBvd25lciB0byAke1dXV19VSUR9IgoJZmkKfQoKZ2V0X2RiX3Bhc3MoKXsKCWlmIFsgLWYgJHtERUZBVUxUX1ZIX1JPT1R9LyR7MX0vLmRiX3Bhc3MgXTsgdGhlbgoJCVNRTF9EQj0kKGdyZXAgLWkgRGF0YWJhc2UgJHtWSF9ST09UfS8uZGJfcGFzcyB8IGF3ayAtRiAnOicgJ3twcmludCAkMn0nIHwgdHIgLWQgJyInKQoJCVNRTF9VU0VSPSQoZ3JlcCAtaSBVc2VybmFtZSAke1ZIX1JPT1R9Ly5kYl9wYXNzIHwgYXdrIC1GICc6JyAne3ByaW50ICQyfScgfCB0ciAtZCAnIicpCgkJU1FMX1BBU1M9JChncmVwIC1pIFBhc3N3b3JkICR7VkhfUk9PVH0vLmRiX3Bhc3MgfCBhd2sgLUYgJzonICd7cHJpbnQgJDJ9JyB8IHRyIC1kICciJykKCWVsc2UKCQllY2hvICdkYiBwYXNzIGZpbGUgY2FuIG5vdCBsb2NhdGUsIHNraXAgd3AtY29uZmlnIHByZS1jb25maWcuJwoJZmkKfQoKc2V0X3ZoX2RvY3Jvb3QoKXsKCWlmIFsgIiR7VkhOQU1FfSIgIT0gJycgXTsgdGhlbgoJICAgIFZIX1JPT1Q9IiR7REVGQVVMVF9WSF9ST09UfS8ke1ZITkFNRX0iCgkgICAgVkhfRE9DX1JPT1Q9IiR7REVGQVVMVF9WSF9ST09UfS8ke1ZITkFNRX0vaHRtbCIKCQlXUF9DT05TVF9DT05GPSIke1ZIX0RPQ19ST09UfS93cC1jb250ZW50L3BsdWdpbnMvbGl0ZXNwZWVkLWNhY2hlL2RhdGEvY29uc3QuZGVmYXVsdC5pbmkiCgllbGlmIFsgLWQgJHtERUZBVUxUX1ZIX1JPT1R9LyR7MX0vaHRtbCBdOyB0aGVuCgkgICAgVkhfUk9PVD0iJHtERUZBVUxUX1ZIX1JPT1R9LyR7MX0iCiAgICAgICAgVkhfRE9DX1JPT1Q9IiR7REVGQVVMVF9WSF9ST09UfS8kezF9L2h0bWwiCgkJV1BfQ09OU1RfQ09ORj0iJHtWSF9ET0NfUk9PVH0vd3AtY29udGVudC9wbHVnaW5zL2xpdGVzcGVlZC1jYWNoZS9kYXRhL2NvbnN0LmRlZmF1bHQuaW5pIgoJZWxzZQoJICAgIGVjaG8gIiR7REVGQVVMVF9WSF9ST09UfS8kezF9L2h0bWwgZG9lcyBub3QgZXhpc3QsIHBsZWFzZSBhZGQgZG9tYWluIGZpcnN0ISBBYm9ydCEiCgkJZXhpdCAxCglmaQkKfQoKY2hlY2tfc3FsX25hdGl2ZSgpewoJbG9jYWwgQ09VTlRFUj0wCglsb2NhbCBMSU1JVF9OVU09MTAwCgl1bnRpbCBbICIkKGN1cmwgLXYgbXlzcWw6MzMwNiAyPiYxIHwgZ3JlcCAtaSAnbmF0aXZlXHxDb25uZWN0ZWQnKSIgXTsgZG8KCQllY2hvICJDb3VudGVyOiAke0NPVU5URVJ9LyR7TElNSVRfTlVNfSIKCQlDT1VOVEVSPSQoKENPVU5URVIrMSkpCgkJaWYgWyAke0NPVU5URVJ9ID0gMTAgXTsgdGhlbgoJCQllY2hvICctLS0gTXlTUUwgaXMgc3RhcnRpbmcsIHBsZWFzZSB3YWl0Li4uIC0tLScKCQllbGlmIFsgJHtDT1VOVEVSfSA9ICR7TElNSVRfTlVNfSBdOyB0aGVuCQoJCQllY2hvICctLS0gTXlTUUwgaXMgdGltZW91dCwgZXhpdCEgLS0tJwoJCQlleGl0IDEKCQlmaQoJCXNsZWVwIDEKCWRvbmUKfQoKaW5zdGFsbF93cF9wbHVnaW4oKXsKICAgIGZvciBQTFVHSU4gaW4gJHtQTFVHSU5MSVNUfTsgZG8KICAgICAgICB3Z2V0IC1xIC1QICR7VkhfRE9DX1JPT1R9L3dwLWNvbnRlbnQvcGx1Z2lucy8gaHR0cHM6Ly9kb3dubG9hZHMud29yZHByZXNzLm9yZy9wbHVnaW4vJHtQTFVHSU59CiAgICAgICAgaWYgWyAkez99ID0gMCBdOyB0aGVuCgkJICAgIGNrX3VuemlwCiAgICAgICAgICAgIHVuemlwIC1xcSAtbyAke1ZIX0RPQ19ST09UfS93cC1jb250ZW50L3BsdWdpbnMvJHtQTFVHSU59IC1kICR7VkhfRE9DX1JPT1R9L3dwLWNvbnRlbnQvcGx1Z2lucy8KICAgICAgICBlbHNlCiAgICAgICAgICAgIGVjaG8gIiR7UExVR0lOTElTVH0gRkFJTEVEIHRvIGRvd25sb2FkIgogICAgICAgIGZpCiAgICBkb25lCiAgICBybSAtZiAke1ZIX0RPQ19ST09UfS93cC1jb250ZW50L3BsdWdpbnMvKi56aXAKfQoKc2V0X2h0YWNjZXNzKCl7CiAgICBpZiBbICEgLWYgJHtWSF9ET0NfUk9PVH0vLmh0YWNjZXNzIF07IHRoZW4gCiAgICAgICAgdG91Y2ggJHtWSF9ET0NfUk9PVH0vLmh0YWNjZXNzCiAgICBmaSAgIAogICAgY2F0IDw8IEVPTSA+ICR7VkhfRE9DX1JPT1R9Ly5odGFjY2VzcwojIEJFR0lOIFdvcmRQcmVzcwo8SWZNb2R1bGUgbW9kX3Jld3JpdGUuYz4KUmV3cml0ZUVuZ2luZSBPbgpSZXdyaXRlQmFzZSAvClJld3JpdGVSdWxlIF5pbmRleFwucGhwJCAtIFtMXQpSZXdyaXRlQ29uZCAle1JFUVVFU1RfRklMRU5BTUV9ICEtZgpSZXdyaXRlQ29uZCAle1JFUVVFU1RfRklMRU5BTUV9ICEtZApSZXdyaXRlUnVsZSAuIC9pbmRleC5waHAgW0xdCjwvSWZNb2R1bGU+CiMgRU5EIFdvcmRQcmVzcwpFT00KfQoKZ2V0X3RoZW1lX25hbWUoKXsKICAgIFRIRU1FX05BTUU9JChncmVwIFdQX0RFRkFVTFRfVEhFTUUgJHtWSF9ET0NfUk9PVH0vd3AtaW5jbHVkZXMvZGVmYXVsdC1jb25zdGFudHMucGhwIHwgZ3JlcCAtdiAnIScgfCBhd2sgLUYgIiciICd7cHJpbnQgJDR9JykKICAgIGVjaG8gIiR7VEhFTUVfTkFNRX0iIHwgZ3JlcCAndHdlbnR5JyA+L2Rldi9udWxsIDI+JjEKICAgIGlmIFsgJHs/fSA9IDAgXTsgdGhlbgogICAgICAgIFRIRU1FPSIke1RIRU1FX05BTUV9IgogICAgZmkKfQoKc2V0X2xzY2FjaGUoKXsgCiAgICBjYXQgPDwgRU9NID4gIiR7V1BfQ09OU1RfQ09ORn0iIAo7CjsgVGhpcyBpcyB0aGUgcHJlZGVmaW5lZCBkZWZhdWx0IExTQ1dQIGNvbmZpZ3VyYXRpb24gZmlsZQo7CjsgQWxsIHRoZSBrZXlzIGFuZCB2YWx1ZXMgcGxlYXNlIHJlZmVyIFxgc3JjL2NvbnN0LmNscy5waHBcYAo7CjsgQ29tbWVudHMgc3RhcnQgd2l0aCBcYDtcYAo7Cjs7IC0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIDs7Cjs7IC0tLS0tLS0tLS0tLS0tICAgICAgICAgIEdlbmVyYWwgICAgICAgICAgICAgIC0tLS0tLS0tLS0tLS0tLS0tIDs7Cjs7IC0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIDs7CjsgT19BVVRPX1VQR1JBREUKYXV0b191cGdyYWRlID0gZmFsc2UKOyBPX0FQSV9LRVkKYXBpX2tleSA9ICcnCjsgT19TRVJWRVJfSVAKc2VydmVyX2lwID0gJycKOyBPX05FV1MKbmV3cyA9IGZhbHNlCjs7IC0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIDs7Cjs7IC0tLS0tLS0tLS0tLS0tICAgICAgICAgICAgICAgQ2FjaGUgICAgICAgICAgIC0tLS0tLS0tLS0tLS0tLS0tIDs7Cjs7IC0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIDs7CmNhY2hlLXByaXYgPSB0cnVlCmNhY2hlLWNvbW1lbnRlciA9IHRydWUKY2FjaGUtcmVzdCA9IHRydWUKY2FjaGUtcGFnZV9sb2dpbiA9IHRydWUKY2FjaGUtZmF2aWNvbiA9IHRydWUKY2FjaGUtcmVzb3VyY2VzID0gdHJ1ZQpjYWNoZS1icm93c2VyID0gZmFsc2UKY2FjaGUtbW9iaWxlID0gZmFsc2UKY2FjaGUtbW9iaWxlX3J1bGVzID0gJ01vYmlsZQpBbmRyb2lkClNpbGsvCktpbmRsZQpCbGFja0JlcnJ5Ck9wZXJhIE1pbmkKT3BlcmEgTW9iaScKY2FjaGUtZXhjX3VzZXJhZ2VudHMgPSAnJwpjYWNoZS1leGNfY29va2llcyA9ICcnCmNhY2hlLWV4Y19xcyA9ICcnCmNhY2hlLWV4Y19jYXQgPSAnJwpjYWNoZS1leGNfdGFnID0gJycKY2FjaGUtZm9yY2VfdXJpID0gJycKY2FjaGUtZm9yY2VfcHViX3VyaSA9ICcnCmNhY2hlLXByaXZfdXJpID0gJycKY2FjaGUtZXhjID0gJycKY2FjaGUtZXhjX3JvbGVzID0gJycKY2FjaGUtZHJvcF9xcyA9ICdmYmNsaWQKZ2NsaWQKdXRtKgpfZ2EnCmNhY2hlLXR0bF9wdWIgPSA2MDQ4MDAKY2FjaGUtdHRsX3ByaXYgPSAxODAwCmNhY2hlLXR0bF9mcm9udHBhZ2UgPSA2MDQ4MDAKY2FjaGUtdHRsX2ZlZWQgPSA2MDQ4MDAKOyBPX0NBQ0hFX1RUTF9SRVNUCmNhY2hlLXR0bF9yZXN0ID0gNjA0ODAwCmNhY2hlLXR0bF9icm93c2VyID0gMzE1NTc2MDAKY2FjaGUtbG9naW5fY29va2llID0gJycKY2FjaGUtdmFyeV9ncm91cCA9ICcnCmNhY2hlLXR0bF9zdGF0dXMgPSAnNDAzIDM2MDAKNDA0IDM2MDAKNTAwIDM2MDAnCjs7IC0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIDs7Cjs7IC0tLS0tLS0tLS0tLS0tICAgICAgICAgICAgICAgUHVyZ2UgICAgICAgICAgIC0tLS0tLS0tLS0tLS0tLS0tIDs7Cjs7IC0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIDs7CjsgT19QVVJHRV9PTl9VUEdSQURFCnB1cmdlLXVwZ3JhZGUgPSB0cnVlCjsgT19QVVJHRV9TVEFMRQpwdXJnZS1zdGFsZSA9IHRydWUKcHVyZ2UtcG9zdF9hbGwgID0gZmFsc2UKcHVyZ2UtcG9zdF9mICAgID0gdHJ1ZQpwdXJnZS1wb3N0X2ggICAgPSB0cnVlCnB1cmdlLXBvc3RfcCAgICA9IHRydWUKcHVyZ2UtcG9zdF9wd3JwID0gdHJ1ZQpwdXJnZS1wb3N0X2EgICAgPSB0cnVlCnB1cmdlLXBvc3RfeSAgICA9IGZhbHNlCnB1cmdlLXBvc3RfbSAgICA9IHRydWUKcHVyZ2UtcG9zdF9kICAgID0gZmFsc2UKcHVyZ2UtcG9zdF90ICAgID0gdHJ1ZQpwdXJnZS1wb3N0X3B0ICAgPSB0cnVlCnB1cmdlLXRpbWVkX3VybHMgPSAnJwpwdXJnZS10aW1lZF91cmxzX3RpbWUgPSAnJwpwdXJnZS1ob29rX2FsbCA9ICdzd2l0Y2hfdGhlbWUKd3BfY3JlYXRlX25hdl9tZW51CndwX3VwZGF0ZV9uYXZfbWVudQp3cF9kZWxldGVfbmF2X21lbnUKY3JlYXRlX3Rlcm0KZWRpdF90ZXJtcwpkZWxldGVfdGVybQphZGRfbGluawplZGl0X2xpbmsKZGVsZXRlX2xpbmsnCjs7IC0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIDs7Cjs7IC0tLS0tLS0tLS0tLS0tICAgICAgICBFU0kgICAgICAgIC0tLS0tLS0tLS0tLS0tLS0tIDs7Cjs7IC0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIDs7CjsgT19FU0kKZXNpID0gZmFsc2UKOyBPX0VTSV9DQUNIRV9BRE1CQVIKZXNpLWNhY2hlX2FkbWJhciA9IHRydWUKOyBPX0VTSV9DQUNIRV9DT01NRk9STQplc2ktY2FjaGVfY29tbWZvcm0gPSB0cnVlCjsgT19FU0lfTk9OQ0UKZXNpLW5vbmNlID0gJ3N0YXRzX25vbmNlCnN1YnNjcmliZV9ub25jZScKOzsgLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0gOzsKOzsgLS0tLS0tLS0tLS0tLS0gICAgIFV0aWxpdGllcyAgICAgLS0tLS0tLS0tLS0tLS0tLS0gOzsKOzsgLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0gOzsKdXRpbC1oZWFydGJlYXQgPSB0cnVlCnV0aWwtaW5zdGFudF9jbGljayA9IGZhbHNlCnV0aWwtY2hlY2tfYWR2Y2FjaGUgPSB0cnVlCnV0aWwtbm9faHR0cHNfdmFyeSA9IGZhbHNlCjs7IC0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIDs7Cjs7IC0tLS0tLS0tLS0tLS0tICAgICAgICAgICAgICAgRGVidWcgICAgICAgICAgIC0tLS0tLS0tLS0tLS0tLS0tIDs7Cjs7IC0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIDs7CjsgT19ERUJVR19ESVNBQkxFX0FMTApkZWJ1Zy1kaXNhYmxlX2FsbCA9IGZhbHNlCjsgT19ERUJVRwpkZWJ1ZyA9IGZhbHNlCjsgT19ERUJVR19JUFMKZGVidWctaXBzID0gJzEyNy4wLjAuMScKOyBPX0RFQlVHX0xFVkVMCmRlYnVnLWxldmVsID0gZmFsc2UKOyBPX0RFQlVHX0ZJTEVTSVpFCmRlYnVnLWZpbGVzaXplID0gMwo7IE9fREVCVUdfQ09PS0lFCmRlYnVnLWNvb2tpZSA9IGZhbHNlCjsgT19ERUJVR19DT0xMQVBTX1FTCmRlYnVnLWNvbGxhcHNfcXMgPSBmYWxzZQo7IE9fREVCVUdfSU5DCmRlYnVnLWluYyA9ICcnCjsgT19ERUJVR19FWEMKZGVidWctZXhjID0gJycKOzsgLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0gOzsKOzsgLS0tLS0tLS0tLS0tLS0gICAgICAgICAgIERCIE9wdG0gICAgIC0tLS0tLS0tLS0tLS0tLS0tIDs7Cjs7IC0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIDs7CjsgT19EQl9PUFRNX1JFVklTSU9OU19NQVgKZGJfb3B0bS1yZXZpc2lvbnNfbWF4ID0gMAo7IE9fREJfT1BUTV9SRVZJU0lPTlNfQUdFCmRiX29wdG0tcmV2aXNpb25zX2FnZSA9IDAKOzsgLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0gOzsKOzsgLS0tLS0tLS0tLS0tLS0gICAgICAgICBIVE1MIE9wdG0gICAgIC0tLS0tLS0tLS0tLS0tLS0tIDs7Cjs7IC0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIDs7CjsgT19PUFRNX0NTU19NSU4Kb3B0bS1jc3NfbWluID0gZmFsc2UKb3B0bS1jc3NfaW5saW5lX21pbiA9IGZhbHNlCjsgT19PUFRNX0NTU19DT01CCm9wdG0tY3NzX2NvbWIgPSBmYWxzZQpvcHRtLWNzc19jb21iX3ByaW9yaXR5ID0gZmFsc2UKOyBPX09QVE1fQ1NTX0hUVFAyCm9wdG0tY3NzX2h0dHAyID0gZmFsc2UKb3B0bS1jc3NfZXhjID0gJycKOyBPX09QVE1fSlNfTUlOCm9wdG0tanNfbWluID0gZmFsc2UKb3B0bS1qc19pbmxpbmVfbWluID0gZmFsc2UKOyBPX09QVE1fSlNfQ09NQgpvcHRtLWpzX2NvbWIgPSBmYWxzZQpvcHRtLWpzX2NvbWJfcHJpb3JpdHkgPSBmYWxzZQo7IE9fT1BUTV9KU19IVFRQMgpvcHRtLWpzX2h0dHAyID0gZmFsc2UKOyBPX09QVE1fRVhDX0pRCm9wdG0tanNfZXhjID0gJycKb3B0bS10dGwgPSA2MDQ4MDAKb3B0bS1odG1sX21pbiA9IGZhbHNlCm9wdG0tcXNfcm0gPSBmYWxzZQpvcHRtLWdnZm9udHNfcm0gPSBmYWxzZQo7IE9fT1BUTV9DU1NfQVNZTkMKb3B0bS1jc3NfYXN5bmMgPSBmYWxzZQo7IE9fT1BUTV9DQ1NTX0dFTgpvcHRtLWNjc3NfZ2VuID0gdHJ1ZQo7IE9fT1BUTV9DQ1NTX0FTWU5DCm9wdG0tY2Nzc19hc3luYyA9IHRydWUKOyBPX09QVE1fQ1NTX0FTWU5DX0lOTElORQpvcHRtLWNzc19hc3luY19pbmxpbmUgPSB0cnVlCjsgT19PUFRNX0NTU19GT05UX0RJU1BMQVkKb3B0bS1jc3NfZm9udF9kaXNwbGF5ID0gZmFsc2UKOyBPX09QVE1fSlNfREVGRVIKb3B0bS1qc19kZWZlciA9IGZhbHNlCjsgT19PUFRNX0pTX0lOTElORV9ERUZFUgpvcHRtLWpzX2lubGluZV9kZWZlciA9IGZhbHNlCm9wdG0tZW1vamlfcm0gPSBmYWxzZQpvcHRtLWV4Y19qcSA9IHRydWUKb3B0bS1nZ2ZvbnRzX2FzeW5jID0gZmFsc2UKb3B0bS1tYXhfc2l6ZSA9IDIKb3B0bS1ybV9jb21tZW50ID0gZmFsc2UKb3B0bS1leGNfcm9sZXMgPSAnJwpvcHRtLWNjc3NfY29uID0gJycKb3B0bS1qc19kZWZlcl9leGMgPSAnJwo7IE9fT1BUTV9ETlNfUFJFRkVUQ0gKb3B0bS1kbnNfcHJlZmV0Y2ggPSAnJwo7IE9fT1BUTV9ETlNfUFJFRkVUQ0hfQ1RSTApvcHRtLWRuc19wcmVmZXRjaF9jdHJsID0gZmFsc2UKb3B0bS1leGMgPSAnJwo7IE9fT1BUTV9DQ1NTX1NFUF9QT1NUVFlQRQpvcHRtLWNjc3Nfc2VwX3Bvc3R0eXBlID0gJycKOyBPX09QVE1fQ0NTU19TRVBfVVJJCm9wdG0tY2Nzc19zZXBfdXJpID0gJycKOzsgLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0gOzsKOzsgLS0tLS0tLS0tLS0tLS0gICAgICAgT2JqZWN0IENhY2hlICAgIC0tLS0tLS0tLS0tLS0tLS0tIDs7Cjs7IC0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIDs7Cm9iamVjdCA9IHRydWUKb2JqZWN0LWtpbmQgPSBmYWxzZQo7b2JqZWN0LWhvc3QgPSAnbG9jYWxob3N0JwpvYmplY3QtaG9zdCA9ICcvdmFyL3d3dy9tZW1jYWNoZWQuc29jaycKO29iamVjdC1wb3J0ID0gMTEyMTEKY2FjaGVfb2JqZWN0X3BvcnQgPSAnJwpvYmplY3QtbGlmZSA9IDM2MApvYmplY3QtcGVyc2lzdGVudCA9IHRydWUKb2JqZWN0LWFkbWluID0gdHJ1ZQpvYmplY3QtdHJhbnNpZW50cyA9IHRydWUKb2JqZWN0LWRiX2lkID0gMApvYmplY3QtdXNlciA9ICcnCm9iamVjdC1wc3dkID0gJycKb2JqZWN0LWdsb2JhbF9ncm91cHMgPSAndXNlcnMKdXNlcmxvZ2lucwp1c2VybWV0YQp1c2VyX21ldGEKc2l0ZS10cmFuc2llbnQKc2l0ZS1vcHRpb25zCnNpdGUtbG9va3VwCmJsb2ctbG9va3VwCmJsb2ctZGV0YWlscwpyc3MKZ2xvYmFsLXBvc3RzCmJsb2ctaWQtY2FjaGUnCm9iamVjdC1ub25fcGVyc2lzdGVudF9ncm91cHMgPSAnY29tbWVudApjb3VudHMKcGx1Z2lucwp3Y19zZXNzaW9uX2lkJwo7OyAtLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLSA7Owo7OyAtLS0tLS0tLS0tLS0tLSAgICAgICAgRGlzY3Vzc2lvbiAgICAgLS0tLS0tLS0tLS0tLS0tLS0gOzsKOzsgLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0gOzsKOyBPX0RJU0NVU1NfQVZBVEFSX0NBQ0hFCmRpc2N1c3MtYXZhdGFyX2NhY2hlID0gZmFsc2UKOyBPX0RJU0NVU1NfQVZBVEFSX0NST04KZGlzY3Vzcy1hdmF0YXJfY3JvbiA9IGZhbHNlCjsgT19ESVNDVVNTX0FWQVRBUl9DQUNIRV9UVEwKZGlzY3Vzcy1hdmF0YXJfY2FjaGVfdHRsID0gNjA0ODAwCjs7IC0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIDs7Cjs7IC0tLS0tLS0tLS0tLS0tICAgICAgICAgICAgICAgIE1lZGlhICAgICAgICAgIC0tLS0tLS0tLS0tLS0tLS0tIDs7Cjs7IC0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIDs7CjsgT19NRURJQV9MQVpZCm1lZGlhLWxhenkgPSBmYWxzZQo7IE9fTUVESUFfTEFaWV9QTEFDRUhPTERFUgptZWRpYS1sYXp5X3BsYWNlaG9sZGVyID0gJycKOyBPX01FRElBX1BMQUNFSE9MREVSX1JFU1AKbWVkaWEtcGxhY2Vob2xkZXJfcmVzcCA9IGZhbHNlCjsgT19NRURJQV9QTEFDRUhPTERFUl9SRVNQX0NPTE9SCm1lZGlhLXBsYWNlaG9sZGVyX3Jlc3BfY29sb3IgPSAnI2NmZDRkYicKOyBPX01FRElBX1BMQUNFSE9MREVSX1JFU1BfR0VORVJBVE9SCm1lZGlhLXBsYWNlaG9sZGVyX3Jlc3BfZ2VuZXJhdG9yID0gZmFsc2UKOyBPX01FRElBX1BMQUNFSE9MREVSX1JFU1BfU1ZHCm1lZGlhLXBsYWNlaG9sZGVyX3Jlc3Bfc3ZnID0gJzxzdmcgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIiB3aWR0aD0ie3dpZHRofSIgaGVpZ2h0PSJ7aGVpZ2h0fSIgdmlld0JveD0iMCAwIHt3aWR0aH0ge2hlaWdodH0iPjxyZWN0IHdpZHRoPSIxMDAlIiBoZWlnaHQ9IjEwMCUiIGZpbGw9Intjb2xvcn0iLz48L3N2Zz4nCjsgT19NRURJQV9QTEFDRUhPTERFUl9MUUlQCm1lZGlhLXBsYWNlaG9sZGVyX2xxaXAgPSBmYWxzZQo7IE9fTUVESUFfUExBQ0VIT0xERVJfTFFJUF9RVUFMCm1lZGlhLXBsYWNlaG9sZGVyX2xxaXBfcXVhbCA9IDQKOyBPX01FRElBX1BMQUNFSE9MREVSX1JFU1BfQVNZTkMKbWVkaWEtcGxhY2Vob2xkZXJfcmVzcF9hc3luYyA9IHRydWUKOyBPX01FRElBX0lGUkFNRV9MQVpZCm1lZGlhLWlmcmFtZV9sYXp5ID0gZmFsc2UKOyBPX01FRElBX0xBWllKU19JTkxJTkUKbWVkaWEtbGF6eWpzX2lubGluZSA9IGZhbHNlCjsgT19NRURJQV9MQVpZX0VYQwptZWRpYS1sYXp5X2V4YyA9ICcnCjsgT19NRURJQV9MQVpZX0NMU19FWEMKbWVkaWEtbGF6eV9jbHNfZXhjID0gJycKOyBPX01FRElBX0xBWllfUEFSRU5UX0NMU19FWEMKbWVkaWEtbGF6eV9wYXJlbnRfY2xzX2V4YyA9ICcnCjsgT19NRURJQV9JRlJBTUVfTEFaWV9DTFNfRVhDCm1lZGlhLWlmcmFtZV9sYXp5X2Nsc19leGMgPSAnJwo7IE9fTUVESUFfSUZSQU1FX0xBWllfUEFSRU5UX0NMU19FWEMKbWVkaWEtaWZyYW1lX2xhenlfcGFyZW50X2Nsc19leGMgPSAnJwo7IE9fTUVESUFfTEFaWV9VUklfRVhDCm1lZGlhLWxhenlfdXJpX2V4YyA9ICcnCjs7IC0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIDs7Cjs7IC0tLS0tLS0tLS0tLS0tICAgICAgICAgSW1hZ2UgT3B0bSAgICAtLS0tLS0tLS0tLS0tLS0tLSA7Owo7OyAtLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLSA7OwppbWdfb3B0bS1hdXRvID0gZmFsc2UKaW1nX29wdG0tY3JvbiA9IHRydWUKaW1nX29wdG0tb3JpID0gdHJ1ZQppbWdfb3B0bS1ybV9ia3VwID0gZmFsc2UKaW1nX29wdG0td2VicCA9IGZhbHNlCmltZ19vcHRtLWxvc3NsZXNzID0gZmFsc2UKaW1nX29wdG0tZXhpZiA9IGZhbHNlCmltZ19vcHRtLXdlYnBfcmVwbGFjZSA9IGZhbHNlCmltZ19vcHRtLXdlYnBfYXR0ciA9ICdpbWcuc3JjCmRpdi5kYXRhLXRodW1iCmltZy5kYXRhLXNyYwpkaXYuZGF0YS1sYXJnZV9pbWFnZQppbWcucmV0aW5hX2xvZ29fdXJsCmRpdi5kYXRhLXBhcmFsbGF4LWltYWdlCnZpZGVvLnBvc3RlcicKaW1nX29wdG0td2VicF9yZXBsYWNlX3NyY3NldCA9IGZhbHNlCmltZ19vcHRtLWpwZ19xdWFsaXR5ID0gODIKOzsgLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0gOzsKOzsgLS0tLS0tLS0tLS0tLS0gICAgICAgICAgICAgICBDcmF3bGVyICAgICAgICAgLS0tLS0tLS0tLS0tLS0tLS0gOzsKOzsgLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0gOzsKY3Jhd2xlciA9IGZhbHNlCmNyYXdsZXItaW5jX3Bvc3RzID0gdHJ1ZQpjcmF3bGVyLWluY19wYWdlcyA9IHRydWUKY3Jhd2xlci1pbmNfY2F0cyA9IHRydWUKY3Jhd2xlci1pbmNfdGFncyA9IHRydWUKY3Jhd2xlci1leGNfY3B0ID0gJycKY3Jhd2xlci1vcmRlcl9saW5rcyA9ICdkYXRlX2Rlc2MnCmNyYXdsZXItdXNsZWVwID0gNTAwCmNyYXdsZXItcnVuX2R1cmF0aW9uID0gNDAwCmNyYXdsZXItcnVuX2ludGVydmFsID0gNjAwCmNyYXdsZXItY3Jhd2xfaW50ZXJ2YWwgPSAzMDI0MDAKY3Jhd2xlci10aHJlYWRzID0gMwpjcmF3bGVyLXRpbWVvdXQgPSAzMApjcmF3bGVyLWxvYWRfbGltaXQgPSAxCjsgT19DUkFXTEVSX1NJVEVNQVAKY3Jhd2xlci1zaXRlbWFwID0gJycKOyBPX0NSQVdMRVJfRFJPUF9ET01BSU4KY3Jhd2xlci1kcm9wX2RvbWFpbiA9IHRydWUKY3Jhd2xlci1yb2xlcyA9ICcnCmNyYXdsZXItY29va2llcyA9ICcnCjs7IC0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIDs7Cjs7IC0tLS0tLS0tLS0tLS0tICAgICAgICAgICAgICAgIE1pc2MgICAgICAgICAgIC0tLS0tLS0tLS0tLS0tLS0tIDs7Cjs7IC0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIDs7CjsgT19NSVNDX0hUQUNDRVNTX0ZST05UCm1pc2MtaHRhY2Nlc3NfZnJvbnQgPSAnJwo7IE9fTUlTQ19IVEFDQ0VTU19CQUNLCm1pc2MtaHRhY2Nlc3NfYmFjayA9ICcnCjsgT19NSVNDX0hFQVJUQkVBVF9GUk9OVAptaXNjLWhlYXJ0YmVhdF9mcm9udCA9IGZhbHNlCjsgT19NSVNDX0hFQVJUQkVBVF9GUk9OVF9UVEwKbWlzYy1oZWFydGJlYXRfZnJvbnRfdHRsID0gNjAKOyBPX01JU0NfSEVBUlRCRUFUX0JBQ0sKbWlzYy1oZWFydGJlYXRfYmFjayA9IGZhbHNlCjsgT19NSVNDX0hFQVJUQkVBVF9CQUNLX1RUTAptaXNjLWhlYXJ0YmVhdF9iYWNrX3R0bCA9IDYwCjsgT19NSVNDX0hFQVJUQkVBVF9FRElUT1IKbWlzYy1oZWFydGJlYXRfZWRpdG9yID0gZmFsc2UKOyBPX01JU0NfSEVBUlRCRUFUX0VESVRPUl9UVEwKbWlzYy1oZWFydGJlYXRfZWRpdG9yX3R0bCA9IDE1Cjs7IC0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIDs7Cjs7IC0tLS0tLS0tLS0tLS0tICAgICAgICAgICAgICAgIENETiAgICAgICAgICAgIC0tLS0tLS0tLS0tLS0tLS0tIDs7Cjs7IC0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIDs7CmNkbiA9IGZhbHNlCmNkbi1vcmkgPSAnJwpjZG4tb3JpX2RpciA9ICcnCmNkbi1leGMgPSAnJwpjZG4tcmVtb3RlX2pxID0gZmFsc2UKY2RuLXF1aWMgPSBmYWxzZQpjZG4tcXVpY19lbWFpbCA9ICcnCmNkbi1xdWljX2tleSA9ICcnCmNkbi1jbG91ZGZsYXJlID0gZmFsc2UKY2RuLWNsb3VkZmxhcmVfZW1haWwgPSAnJwpjZG4tY2xvdWRmbGFyZV9rZXkgPSAnJwpjZG4tY2xvdWRmbGFyZV9uYW1lID0gJycKY2RuLWNsb3VkZmxhcmVfem9uZSA9ICcnCjsgXGBjZG4tbWFwcGluZ1xgIG5lZWRzIHRvIGJlIHB1dCBpbiB0aGUgZW5kIHdpdGggYSBzZWN0aW9uIHRhZwo7OyAtLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLSA7Owo7OyAtLS0tLS0tLS0tLS0tLSAgICAgICAgICAgICAgICBDRE4gMiAgICAgICAgICAtLS0tLS0tLS0tLS0tLS0tLSA7Owo7OyAtLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLSA7Owo7IDwtLS0tLS0tLS0tLS0gQ0ROIE1hcHBpbmcgRXhhbXBsZSBCRUdJTiAtLS0tLS0tLS0tLS0tLS0tLS0tLT4KOyBOZWVkIHRvIGtlZXAgdGhlIHNlY3Rpb24gdGFnIFxgW2Nkbi1tYXBwaW5nXVxgIGJlZm9yZSBsaXN0Lgo7CjsgTk9URSAxKSBOZWVkIHRvIHNldCBhbGwgY2hpbGQgb3B0aW9ucyB0byBtYWtlIGFsbCByZXNvdXJjZXMgdG8gYmUgcmVwbGFjZWQgd2l0aG91dCBtaXNzaW5nLgo7IE5PVEUgMikgXGB1cmxbbl1cYCBvcHRpb24gbXVzdCBoYXZlIHRvIGVuYWJsZSB0aGUgcm93IHNldHRpbmcgb2YgXGBuXGAuCjsgTk9URSAzKSBUaGlzIHNlY3Rpb24gbmVlZHMgdG8gYmUgcHV0IGluIHRoZSBlbmQgb2YgdGhpcyAuaW5pIGZpbGUKOwo7IFRvIGVuYWJsZSB0aGUgMm5kIG1hcHBpbmcgcmVjb3JkIGJ5IGRlZmF1bHQsIHBsZWFzZSByZW1vdmUgdGhlIFxgOztcYCBpbiB0aGUgcmVsYXRlZCBsaW5lcy4KW2Nkbi1tYXBwaW5nXQp1cmxbMF0gPSAnJwppbmNfanNbMF0gPSB0cnVlCmluY19jc3NbMF0gPSB0cnVlCmluY19pbWdbMF0gPSB0cnVlCmZpbGV0eXBlWzBdID0gJy5hYWMKLmNzcwouZW90Ci5naWYKLmpwZWcKLmpzCi5qcGcKLmxlc3MKLm1wMwoubXA0Ci5vZ2cKLm90ZgoucGRmCi5wbmcKLnN2ZwoudHRmCi53b2ZmJwo7O3VybFsxXSA9ICdodHRwczovLzJuZF9DRE5fdXJsLmNvbS8nCjs7ZmlsZXR5cGVbMV0gPSAnLndlYm0nCjsgPC0tLS0tLS0tLS0tLSBDRE4gTWFwcGluZyBFeGFtcGxlIEVORCAtLS0tLS0tLS0tLS0tLS0tLS0+CkVPTQoKICAgIGlmIFsgISAtZiAke1ZIX0RPQ19ST09UfS93cC1jb250ZW50L3RoZW1lcy8ke1RIRU1FfS9mdW5jdGlvbnMucGhwLmJrIF07IHRoZW4gCiAgICAgICAgY3AgJHtWSF9ET0NfUk9PVH0vd3AtY29udGVudC90aGVtZXMvJHtUSEVNRX0vZnVuY3Rpb25zLnBocCAke1ZIX0RPQ19ST09UfS93cC1jb250ZW50L3RoZW1lcy8ke1RIRU1FfS9mdW5jdGlvbnMucGhwLmJrCiAgICAgICAgY2tfZWQKICAgICAgICBlZCAke1ZIX0RPQ19ST09UfS93cC1jb250ZW50L3RoZW1lcy8ke1RIRU1FfS9mdW5jdGlvbnMucGhwIDw8IEVORCA+Pi9kZXYvbnVsbCAyPiYxCjJpCnJlcXVpcmVfb25jZSggV1BfQ09OVEVOVF9ESVIuJy8uLi93cC1hZG1pbi9pbmNsdWRlcy9wbHVnaW4ucGhwJyApOwpcJHBhdGggPSAnbGl0ZXNwZWVkLWNhY2hlL2xpdGVzcGVlZC1jYWNoZS5waHAnIDsKaWYgKCFpc19wbHVnaW5fYWN0aXZlKCBcJHBhdGggKSkgewogICAgYWN0aXZhdGVfcGx1Z2luKCBcJHBhdGggKSA7CiAgICByZW5hbWUoIF9fRklMRV9fIC4gJy5iaycsIF9fRklMRV9fICk7Cn0KLgp3CnEKRU5ECiAgICBmaQp9CgpwcmVpbnN0YWxsX3dvcmRwcmVzcygpewoJaWYgWyAiJHtWSE5BTUV9IiAhPSAnJyBdOyB0aGVuCgkgICAgZ2V0X2RiX3Bhc3MgJHtWSE5BTUV9CgllbHNlCgkJZ2V0X2RiX3Bhc3MgJHtET01BSU59CglmaQkKCWlmIFsgISAtZiAke1ZIX0RPQ19ST09UfS93cC1jb25maWcucGhwIF0gJiYgWyAtZiAke1ZIX0RPQ19ST09UfS93cC1jb25maWctc2FtcGxlLnBocCBdOyB0aGVuCgkJY3AgJHtWSF9ET0NfUk9PVH0vd3AtY29uZmlnLXNhbXBsZS5waHAgJHtWSF9ET0NfUk9PVH0vd3AtY29uZmlnLnBocAoJCU5FV0RCUFdEPSJkZWZpbmUoJ0RCX1BBU1NXT1JEJywgJyR7U1FMX1BBU1N9Jyk7IgoJCWxpbmVjaGFuZ2UgJ0RCX1BBU1NXT1JEJyAke1ZIX0RPQ19ST09UfS93cC1jb25maWcucGhwICIke05FV0RCUFdEfSIKCQlORVdEQlBXRD0iZGVmaW5lKCdEQl9VU0VSJywgJyR7U1FMX1VTRVJ9Jyk7IgoJCWxpbmVjaGFuZ2UgJ0RCX1VTRVInICR7VkhfRE9DX1JPT1R9L3dwLWNvbmZpZy5waHAgIiR7TkVXREJQV0R9IgoJCU5FV0RCUFdEPSJkZWZpbmUoJ0RCX05BTUUnLCAnJHtTUUxfREJ9Jyk7IgoJCWxpbmVjaGFuZ2UgJ0RCX05BTUUnICR7VkhfRE9DX1JPT1R9L3dwLWNvbmZpZy5waHAgIiR7TkVXREJQV0R9IgogICAgICAgICNORVdEQlBXRD0iZGVmaW5lKCdEQl9IT1NUJywgJyR7UFVCX0lQfScpOyIKCQlORVdEQlBXRD0iZGVmaW5lKCdEQl9IT1NUJywgJyR7REJfSE9TVH0nKTsiCgkJbGluZWNoYW5nZSAnREJfSE9TVCcgJHtWSF9ET0NfUk9PVH0vd3AtY29uZmlnLnBocCAiJHtORVdEQlBXRH0iCgllbGlmIFsgLWYgJHtWSF9ET0NfUk9PVH0vd3AtY29uZmlnLnBocCBdOyB0aGVuCgkJZWNobyAiJHtWSF9ET0NfUk9PVH0vd3AtY29uZmlnLnBocCBhbHJlYWR5IGV4aXN0LCBleGl0ICEiCgkJZXhpdCAxCgllbHNlCgkJZWNobyAnU2tpcCEnCgkJZXhpdCAyCglmaSAKfQoKYXBwX3dvcmRwcmVzc19kbCgpewoJaWYgWyAhIC1mICIke1ZIX0RPQ19ST09UfS93cC1jb25maWcucGhwIiBdICYmIFsgISAtZiAiJHtWSF9ET0NfUk9PVH0vd3AtY29uZmlnLXNhbXBsZS5waHAiIF07IHRoZW4KCQl3cCBjb3JlIGRvd25sb2FkIFwKCQkJLS1hbGxvdy1yb290IFwKCQkJLS1xdWlldAoJZWxzZQoJICAgIGVjaG8gJ3dvcmRwcmVzcyBhbHJlYWR5IGV4aXN0LCBhYm9ydCEnCgkJZXhpdCAxCglmaQp9CgpjaGFuZ2Vfb3duZXIoKXsKCQlpZiBbICIke1ZITkFNRX0iICE9ICcnIF07IHRoZW4KCQkgICAgY2hvd24gLVIgJHtXV1dfVUlEfToke1dXV19HSUR9ICR7REVGQVVMVF9WSF9ST09UfS8ke1ZITkFNRX0gCgkJZWxzZQoJCSAgICBjaG93biAtUiAke1dXV19VSUR9OiR7V1dXX0dJRH0gJHtERUZBVUxUX1ZIX1JPT1R9LyR7RE9NQUlOfQoJCWZpCn0KCm1haW4oKXsKCXNldF92aF9kb2Nyb290ICR7RE9NQUlOfQoJZ2V0X293bmVyCgljZCAke1ZIX0RPQ19ST09UfQoJaWYgWyAiJHtBUFBfTkFNRX0iID0gJ3dvcmRwcmVzcycgXSB8fCBbICIke0FQUF9OQU1FfSIgPSAnd3AnIF07IHRoZW4KCQljaGVja19zcWxfbmF0aXZlCgkJYXBwX3dvcmRwcmVzc19kbAoJCXByZWluc3RhbGxfd29yZHByZXNzCgkJaW5zdGFsbF93cF9wbHVnaW4KCQlzZXRfaHRhY2Nlc3MKCQlnZXRfdGhlbWVfbmFtZQoJCXNldF9sc2NhY2hlCgkJY2hhbmdlX293bmVyCgkJZXhpdCAwCgllbHNlCgkJZWNobyAiQVBQOiAke0FQUF9OQU1FfSBub3Qgc3VwcG9ydCwgZXhpdCEiCgkJZXhpdCAxCQoJZmkKfQoKY2hlY2tfaW5wdXQgJHsxfQp3aGlsZSBbICEgLXogIiR7MX0iIF07IGRvCgljYXNlICR7MX0gaW4KCQktW2hIXSB8IC1oZWxwIHwgLS1oZWxwKQoJCQloZWxwX21lc3NhZ2UKCQkJOzsKCQktW2FBXSB8IC1hcHAgfCAtLWFwcCkgc2hpZnQKCQkJY2hlY2tfaW5wdXQgIiR7MX0iCgkJCUFQUF9OQU1FPSIkezF9IgoJCQk7OwoJCS1bZERdIHwgLWRvbWFpbiB8IC0tZG9tYWluKSBzaGlmdAoJCQljaGVja19pbnB1dCAiJHsxfSIKCQkJRE9NQUlOPSIkezF9IgoJCQk7OwoJCS12aG5hbWUgfCAtLXZobmFtZSkgc2hpZnQKCQkJVkhOQU1FPSIkezF9IgoJCQk7OwkgICAgICAgCgkJKikgCgkJCWhlbHBfbWVzc2FnZQoJCQk7OyAgICAgICAgICAgICAgCgllc2FjCglzaGlmdApkb25lCm1haW4K"
                    },

                    new File
                    {
                        path= "bin/container/",
                        filename="certhookctl.sh",
                        content= "IyEvYmluL2Jhc2gKQk9UQ1JPTj0nL3Zhci9zcG9vbC9jcm9uL2Nyb250YWJzL3Jvb3QnCgpjZXJ0X2hvb2soKXsKICAgIGdyZXAgJ2FjbWUnICR7Qk9UQ1JPTn0gPi9kZXYvbnVsbAogICAgaWYgWyAkez99ID0gMCBdOyB0aGVuCiAgICAgICAgZ3JlcCAnbHN3c2N0cmwnICR7Qk9UQ1JPTn0gPi9kZXYvbnVsbAogICAgICAgIGlmIFsgJHs/fSA9IDAgXTsgdGhlbgogICAgICAgICAgICBlY2hvICdIb29rIGFscmVhZHkgZXhpc3QsIHNraXAhJwogICAgICAgIGVsc2UKICAgICAgICAgICAgc2VkIC1pICdzLy0tY3Jvbi8tLWNyb24gLS1yZW5ldy1ob29rICJcL3VzclwvbG9jYWxcL2xzd3NcL2JpblwvbHN3c2N0cmwgcmVzdGFydCIvZycgJHtCT1RDUk9OfQogICAgICAgIGZpICAgIAogICAgZWxzZQogICAgICAgIGVjaG8gIltYXSAke0JPVENST059IGRvZXMgbm90IGV4aXN0LCBwbGVhc2UgY2hlY2sgaXQgbGF0ZXIhIgogICAgZmkKfQoKY2VydF9ob29r"
                    },

                    new File
                    {
                        path= "bin/container/",
                        filename="domainctl.sh",
                        content= "IyEvdXNyL2Jpbi9lbnYgYmFzaApDS19SRVNVTFQ9JycKTFNESVI9Jy91c3IvbG9jYWwvbHN3cycKTFNfSFRUUERfQ09ORj0iJHtMU0RJUn0vY29uZi9odHRwZF9jb25maWcueG1sIgpPTFNfSFRUUERfQ09ORj0iJHtMU0RJUn0vY29uZi9odHRwZF9jb25maWcuY29uZiIKRVBBQ0U9JyAgICAgICAgJwoKZWNob3coKXsKICAgIEZMQUc9JHsxfQogICAgc2hpZnQKICAgIGVjaG8gLWUgIlwwMzNbMW0ke0VQQUNFfSR7RkxBR31cMDMzWzBtJHtAfSIKfQoKaGVscF9tZXNzYWdlKCl7CiAgICBlY2hvIC1lICJcMDMzWzFtT1BUSU9OU1wwMzNbMG0iCiAgICBlY2hvdyAnLUEsIC0tYWRkIFtET01BSU5fTkFNRV0nCiAgICBlY2hvICIke0VQQUNFfSR7RVBBQ0V9V2lsbCBhZGQgZG9tYWluIHRvIGxpc3RlbmVyIGFuZCBjcmVhdCBhIHZpcnR1YWwgaG9zdCBmcm9tIHRlbXBsYXRlIgogICAgZWNob3cgJy1ELCAtLWRlbCBbRE9NQUlOX05BTUVdJwogICAgZWNobyAiJHtFUEFDRX0ke0VQQUNFfVdpbGwgZGVsZXRlIGRvbWFpbiBmcm9tIGxpc3RlbmVyIgogICAgZWNob3cgJy1ILCAtLWhlbHAnCiAgICBlY2hvICIke0VQQUNFfSR7RVBBQ0V9RGlzcGxheSBoZWxwLiIgICAgCn0KCmNoZWNrX2xzdigpewogICAgaWYgWyAtZiAke0xTRElSfS9iaW4vb3BlbmxpdGVzcGVlZCBdOyB0aGVuCiAgICAgICAgTFNWPSdvcGVubGl0ZXNwZWVkJwogICAgZWxpZiBbIC1mICR7TFNESVJ9L2Jpbi9saXRlc3BlZWQgXTsgdGhlbgogICAgICAgIExTVj0nbHN3cycKICAgIGVsc2UKICAgICAgICBlY2hvICdWZXJzaW9uIG5vdCBleGlzdCwgYWJvcnQhJwogICAgICAgIGV4aXQgMSAgICAgCiAgICBmaQp9Cgpkb3RfZXNjYXBlKCl7CiAgICBFU0NBUEU9JChlY2hvICR7MX0gfCBzZWQgJ3MvXC4vXFwuL2cnKQp9ICAKCmNoZWNrX2R1cGxpY2F0ZSgpewogICAgQ0tfUkVTVUxUPSQoZ3JlcCAtRSAiJHsxfSIgJHsyfSkKfQoKZnN0X21hdGNoX2xpbmUoKXsKICAgIEZJUlNUX0xJTkVfTlVNPSQoZ3JlcCAtbiAtbSAxICR7MX0gJHsyfSB8IGF3ayAtRiAnOicgJ3twcmludCAkMX0nKQp9CmZzdF9tYXRjaF9hZnRlcigpewogICAgRklSU1RfTlVNX0FGVEVSPSQodGFpbCAtbiArJHsxfSAkezJ9IHwgZ3JlcCAtbiAtbSAxICR7M30gfCBhd2sgLUYgJzonICd7cHJpbnQgJDF9JykKfQpsc3RfbWF0Y2hfbGluZSgpewogICAgZnN0X21hdGNoX2FmdGVyICR7MX0gJHsyfSAkezN9CiAgICBMQVNUX0xJTkVfTlVNPSQoKCR7RklSU1RfTElORV9OVU19KyR7RklSU1RfTlVNX0FGVEVSfS0xKSkKfQoKY2hlY2tfaW5wdXQoKXsKICAgIGlmIFsgLXogIiR7MX0iIF07IHRoZW4KICAgICAgICBoZWxwX21lc3NhZ2UKICAgICAgICBleGl0IDEKICAgIGZpCn0KCmNoZWNrX3d3dygpewogICAgQ0hFQ0tfV1dXPSQoZWNobyAkezF9IHwgY3V0IC1jMS00KQogICAgaWYgW1sgJHtDSEVDS19XV1d9ID09IHd3dy4gXV0gOyB0aGVuCiAgICAgICAgZWNobyAnd3d3IGRvbWFpbiBzaG91ZG50IGJlIHBhc3NlZCEnCiAgICAgICAgZXhpdCAxCiAgICBmaQp9Cgp3d3dfZG9tYWluKCl7CiAgICBjaGVja193d3cgJHsxfQogICAgV1dXX0RPTUFJTj0kKGVjaG8gd3d3LiR7MX0pCn0KCmFkZF9sc19kb21haW4oKXsKICAgIGZzdF9tYXRjaF9saW5lICdkb2NrZXIueG1sPC90ZW1wbGF0ZUZpbGU+JyAke0xTX0hUVFBEX0NPTkZ9CiAgICBORVdOVU09JCgoRklSU1RfTElORV9OVU0rMikpCiAgICBzZWQgLWkgIiR7TkVXTlVNfWkgXCBcIFwgXCBcIFwgPG1lbWJlcj5cbiBcIFwgXCBcIFwgXCBcIDx2aE5hbWU+JHtET01BSU59PC92aE5hbWU+XG4gXCBcIFwgXCBcIFwgXCA8dmhEb21haW4+JHtET01BSU59LCR7V1dXX0RPTUFJTn08L3ZoRG9tYWluPlxuIFwgXCBcIFwgXCBcIDwvbWVtYmVyPiIgJHtMU19IVFRQRF9DT05GfQp9CgphZGRfb2xzX2RvbWFpbigpewogICAgcGVybCAtMDc3NyAtcCAtaSAtZSAncy8odmhUZW1wbGF0ZSBkb2NrZXIgXHtbXn1dKylcfSooXi4qbGlzdGVuZXJzLiokKS9cMSQyCiAgbWVtYmVyICcke0RPTUFJTn0nIHsKICAgIHZoRG9tYWluICAgICAgICAgICAgICAnJHtET01BSU59LCR7V1dXX0RPTUFJTn0nCiAgfS9nbWknICR7T0xTX0hUVFBEX0NPTkZ9Cn0KCmFkZF9kb21haW4oKXsKICAgIGNoZWNrX2xzdgogICAgZG90X2VzY2FwZSAkezF9CiAgICBET01BSU49JHtFU0NBUEV9CiAgICB3d3dfZG9tYWluICR7MX0KICAgIGlmIFsgIiR7TFNWfSIgPSAnbHN3cycgXTsgdGhlbgogICAgICAgIGNoZWNrX2R1cGxpY2F0ZSAidmhEb21haW4uKiR7RE9NQUlOfSIgJHtMU19IVFRQRF9DT05GfQogICAgICAgIGlmIFsgIiR7Q0tfUkVTVUxUfSIgIT0gJycgXTsgdGhlbgogICAgICAgICAgICBlY2hvICIjIEl0IGFwcGVhcnMgdGhlIGRvbWFpbiBhbHJlYWR5IGV4aXN0ISBDaGVjayB0aGUgJHtMU19IVFRQRF9DT05GfSBpZiB5b3UgYmVsaWV2ZSB0aGlzIGlzIGEgbWlzdGFrZSEiCiAgICAgICAgICAgIGV4aXQgMQogICAgICAgIGZpICAgIAogICAgZWxpZiBbICIke0xTVn0iID0gJ29wZW5saXRlc3BlZWQnIF07IHRoZW4KICAgICAgICBjaGVja19kdXBsaWNhdGUgIm1lbWJlci4qJHtET01BSU59IiAke09MU19IVFRQRF9DT05GfQogICAgICAgIGlmIFsgIiR7Q0tfUkVTVUxUfSIgIT0gJycgXTsgdGhlbgogICAgICAgICAgICBlY2hvICIjIEl0IGFwcGVhcnMgdGhlIGRvbWFpbiBhbHJlYWR5IGV4aXN0ISBDaGVjayB0aGUgJHtPTFNfSFRUUERfQ09ORn0gaWYgeW91IGJlbGlldmUgdGhpcyBpcyBhIG1pc3Rha2UhIgogICAgICAgICAgICBleGl0IDEKICAgICAgICBmaSAgICAgICAgCiAgICBmaQogICAgYWRkX2xzX2RvbWFpbgogICAgYWRkX29sc19kb21haW4KfQoKZGVsX2xzX2RvbWFpbigpewogICAgZnN0X21hdGNoX2xpbmUgIjx2aE5hbWU+KiR7MX0iICR7TFNfSFRUUERfQ09ORn0KICAgIEZJUlNUX0xJTkVfTlVNPSQoKEZJUlNUX0xJTkVfTlVNLTEpKQogICAgbHN0X21hdGNoX2xpbmUgJHtGSVJTVF9MSU5FX05VTX0gJHtMU19IVFRQRF9DT05GfSAnPC9tZW1iZXI+JwogICAgc2VkIC1pICIke0ZJUlNUX0xJTkVfTlVNfSwke0xBU1RfTElORV9OVU19ZCIgJHtMU19IVFRQRF9DT05GfQp9CgpkZWxfb2xzX2RvbWFpbigpewogICAgZnN0X21hdGNoX2xpbmUgJHsxfSAke09MU19IVFRQRF9DT05GfQogICAgbHN0X21hdGNoX2xpbmUgJHtGSVJTVF9MSU5FX05VTX0gJHtPTFNfSFRUUERfQ09ORn0gJ30nCiAgICBzZWQgLWkgIiR7RklSU1RfTElORV9OVU19LCR7TEFTVF9MSU5FX05VTX1kIiAke09MU19IVFRQRF9DT05GfSAgICAKfQoKZGVsX2RvbWFpbigpewogICAgY2hlY2tfbHN2CiAgICBkb3RfZXNjYXBlICR7MX0KICAgIERPTUFJTj0ke0VTQ0FQRX0KICAgIGlmIFsgIiR7TFNWfSIgPSAnbHN3cycgXTsgdGhlbgogICAgICAgIGNoZWNrX2R1cGxpY2F0ZSAidmhEb21haW4uKiR7RE9NQUlOfSIgJHtMU19IVFRQRF9DT05GfQogICAgICAgIGlmIFsgIiR7Q0tfUkVTVUxUfSIgPSAnJyBdOyB0aGVuCiAgICAgICAgICAgIGVjaG8gIiMgRG9tYWluIG5vbi1leGlzdCEgQ2hlY2sgdGhlICR7TFNfSFRUUERfQ09ORn0gaWYgeW91IGJlbGlldmUgdGhpcyBpcyBhIG1pc3Rha2UhIgogICAgICAgICAgICBleGl0IDEKICAgICAgICBmaQogICAgZWxpZiBbICIke0xTVn0iID0gJ29wZW5saXRlc3BlZWQnIF07IHRoZW4KICAgICAgICBjaGVja19kdXBsaWNhdGUgIm1lbWJlci4qJHtET01BSU59IiAke09MU19IVFRQRF9DT05GfQogICAgICAgIGlmIFsgIiR7Q0tfUkVTVUxUfSIgPSAnJyBdOyB0aGVuCiAgICAgICAgICAgIGVjaG8gIiMgRG9tYWluIG5vbi1leGlzdCEgQ2hlY2sgdGhlICR7T0xTX0hUVFBEX0NPTkZ9IGlmIHlvdSBiZWxpZXZlIHRoaXMgaXMgYSBtaXN0YWtlISIKICAgICAgICAgICAgZXhpdCAxCiAgICAgICAgZmkgICAgICAgIAogICAgZmkKICAgIGRlbF9sc19kb21haW4gJHsxfQogICAgZGVsX29sc19kb21haW4gJHsxfQp9CgpjaGVja19pbnB1dCAkezF9CndoaWxlIFsgISAteiAiJHsxfSIgXTsgZG8KICAgIGNhc2UgJHsxfSBpbgogICAgICAgIC1baEhdIHwgLWhlbHAgfCAtLWhlbHApCiAgICAgICAgICAgIGhlbHBfbWVzc2FnZQogICAgICAgICAgICA7OwogICAgICAgIC1bYUFdIHwgLWFkZCB8IC0tYWRkKSBzaGlmdAogICAgICAgICAgICBhZGRfZG9tYWluICR7MX0KICAgICAgICAgICAgOzsKICAgICAgICAtW2REXSB8IC1kZWwgfCAtLWRlbCB8IC0tZGVsZXRlKSBzaGlmdAogICAgICAgICAgICBkZWxfZG9tYWluICR7MX0KICAgICAgICAgICAgOzsgICAgICAgICAgCiAgICAgICAgKikgCiAgICAgICAgICAgIGhlbHBfbWVzc2FnZQogICAgICAgICAgICA7OwogICAgZXNhYwogICAgc2hpZnQKZG9uZQ=="
                    },

                    new File
                    {
                        path= "bin/container/",
                        filename="owaspctl.sh",
                        content= "IyEvYmluL2Jhc2gKTFNESVI9Jy91c3IvbG9jYWwvbHN3cycKT1dBU1BfRElSPSIke0xTRElSfS9jb25mL293YXNwIgpSVUxFX0ZJTEU9J21vZHNlY19pbmNsdWRlcy5jb25mJwpMU19IVFRQRF9DT05GPSIke0xTRElSfS9jb25mL2h0dHBkX2NvbmZpZy54bWwiCk9MU19IVFRQRF9DT05GPSIke0xTRElSfS9jb25mL2h0dHBkX2NvbmZpZy5jb25mIgpFUEFDRT0nICAgICAgICAnCgplY2hvdygpewogICAgRkxBRz0kezF9CiAgICBzaGlmdAogICAgZWNobyAtZSAiXDAzM1sxbSR7RVBBQ0V9JHtGTEFHfVwwMzNbMG0ke0B9Igp9CgpoZWxwX21lc3NhZ2UoKXsKICAgIGVjaG8gLWUgIlwwMzNbMW1PUFRJT05TXDAzM1swbSIKICAgIGVjaG93ICctRSwgLS1lbmFibGUnCiAgICBlY2hvICIke0VQQUNFfSR7RVBBQ0V9V2lsbCBFbmFibGUgbW9kX3NlY3VyZSBtb2R1bGUgd2l0aCBsYXRlc3QgT1dBU1AgdmVyc2lvbiBvZiBydWxlcyIKICAgIGVjaG93ICctRCwgLS1kaXNhYmxlJwogICAgZWNobyAiJHtFUEFDRX0ke0VQQUNFfVdpbGwgRGlzYWJsZSBtb2Rfc2VjdXJlIG1vZHVsZSB3aXRoIGxhdGVzdCBPV0FTUCB2ZXJzaW9uIG9mIHJ1bGVzIiAKICAgIGVjaG93ICctSCwgLS1oZWxwJwogICAgZWNobyAiJHtFUEFDRX0ke0VQQUNFfURpc3BsYXkgaGVscCBhbmQgZXhpdC4iICAgICAgIAogICAgZXhpdCAwCn0KCmNoZWNrX2xzdigpewogICAgaWYgWyAtZiAke0xTRElSfS9iaW4vb3BlbmxpdGVzcGVlZCBdOyB0aGVuCiAgICAgICAgTFNWPSdvcGVubGl0ZXNwZWVkJwogICAgZWxpZiBbIC1mICR7TFNESVJ9L2Jpbi9saXRlc3BlZWQgXTsgdGhlbgogICAgICAgIExTVj0nbHN3cycKICAgIGVsc2UKICAgICAgICBlY2hvICdWZXJzaW9uIG5vdCBleGlzdCwgYWJvcnQhJwogICAgICAgIGV4aXQgMSAgICAgCiAgICBmaQp9CgpjaGVja19pbnB1dCgpewogICAgaWYgWyAteiAiJHsxfSIgXTsgdGhlbgogICAgICAgIGhlbHBfbWVzc2FnZQogICAgICAgIGV4aXQgMQogICAgZmkKfQoKbWtfb3dhc3BfZGlyKCl7CiAgICBpZiBbIC1kICR7T1dBU1BfRElSfSBdIDsgdGhlbgogICAgICAgIHJtIC1yZiAke09XQVNQX0RJUn0KICAgIGZpCiAgICBta2RpciAtcCAke09XQVNQX0RJUn0KICAgIGlmIFsgJHs/fSAtbmUgMCBdIDsgdGhlbgogICAgICAgIGVjaG8gIlVuYWJsZSB0byBjcmVhdGUgZGlyZWN0b3J5OiAke09XQVNQX0RJUn0sIGV4aXQhIgogICAgICAgIGV4aXQgMQogICAgZmkKfQoKZnN0X21hdGNoX2xpbmUoKXsKICAgIEZJUlNUX0xJTkVfTlVNPSQoZ3JlcCAtbiAtbSAxICIkezF9IiAkezJ9IHwgYXdrIC1GICc6JyAne3ByaW50ICQxfScpCn0KZnN0X21hdGNoX2FmdGVyKCl7CiAgICBGSVJTVF9OVU1fQUZURVI9JCh0YWlsIC1uICskezF9ICR7Mn0gfCBncmVwIC1uIC1tIDEgJHszfSB8IGF3ayAtRiAnOicgJ3twcmludCAkMX0nKQp9CmxzdF9tYXRjaF9saW5lKCl7CiAgICBmc3RfbWF0Y2hfYWZ0ZXIgJHsxfSAkezJ9ICR7M30KICAgIExBU1RfTElORV9OVU09JCgoJHtGSVJTVF9MSU5FX05VTX0rJHtGSVJTVF9OVU1fQUZURVJ9LTEpKQp9CgplbmFibGVfb2xzX21vZHNlYygpewogICAgZ3JlcCAnbW9kdWxlIG1vZF9zZWN1cml0eSB7JyAke09MU19IVFRQRF9DT05GfSA+L2Rldi9udWxsIDI+JjEKICAgIGlmIFsgJHs/fSAtZXEgMCBdIDsgdGhlbgogICAgICAgIGVjaG8gIkFscmVhZHkgY29uZmlndXJlZCBmb3IgbW9kc2VjdXJpdHkuIgogICAgZWxzZQogICAgICAgIGVjaG8gJ0VuYWJsZSBtb2RzZWN1cml0eScKICAgICAgICBzZWQgLWkgInM9bW9kdWxlIGNhY2hlPW1vZHVsZSBtb2Rfc2VjdXJpdHkge1xubW9kc2VjdXJpdHkgIG9uXAogICAgICAgIFxubW9kc2VjdXJpdHlfcnVsZXMgXGBcblNlY1J1bGVFbmdpbmUgT25cblxgXG5tb2RzZWN1cml0eV9ydWxlc19maWxlIFwKICAgICAgICAke09XQVNQX0RJUn0vJHtSVUxFX0ZJTEV9XG4gIGxzX2VuYWJsZWQgICAgICAgICAgICAgIDFcbn1cCiAgICAgICAgXG5cbm1vZHVsZSBjYWNoZT0iICR7T0xTX0hUVFBEX0NPTkZ9CiAgICBmaSAgICAKfQoKZW5hYmxlX2xzX21vZHNlYygpewogICAgZ3JlcCAnPGVuYWJsZUNlbnNvcnNoaXA+MTwvZW5hYmxlQ2Vuc29yc2hpcD4nICR7TFNfSFRUUERfQ09ORn0gPi9kZXYvbnVsbCAyPiYxCiAgICBpZiBbICR7P30gLWVxIDAgXSA7IHRoZW4KICAgICAgICBlY2hvICJMU1dTIGFscmVhZHkgY29uZmlndXJlZCBmb3IgbW9kc2VjdXJpdHkiCiAgICBlbHNlCiAgICAgICAgZWNobyAnRW5hYmxlIG1vZHNlY3VyaXR5JwogICAgICAgIHNlZCAtaSBcCiAgICAgICAgInM9PGVuYWJsZUNlbnNvcnNoaXA+MDwvZW5hYmxlQ2Vuc29yc2hpcD49PGVuYWJsZUNlbnNvcnNoaXA+MTwvZW5hYmxlQ2Vuc29yc2hpcD49IiAke0xTX0hUVFBEX0NPTkZ9CiAgICAgICAgc2VkIC1pIFwKICAgICAgICAicz08L2NlbnNvcnNoaXBDb250cm9sPj08L2NlbnNvcnNoaXBDb250cm9sPlxuXAogICAgICAgIDxjZW5zb3JzaGlwUnVsZVNldD5cblwKICAgICAgICA8bmFtZT5Nb2RTZWM8L25hbWU+XG5cCiAgICAgICAgPGVuYWJsZWQ+MTwvZW5hYmxlZD5cblwKICAgICAgICA8cnVsZVNldD5pbmNsdWRlICR7T1dBU1BfRElSfS9tb2RzZWNfaW5jbHVkZXMuY29uZjwvcnVsZVNldD5cblwKICAgICAgICA8L2NlbnNvcnNoaXBSdWxlU2V0Pj0iICR7TFNfSFRUUERfQ09ORn0KICAgIGZpCn0KCmVuYWJsZV9tb2RzZWMoKXsKICAgIGlmIFsgIiR7TFNWfSIgPSAnbHN3cycgXTsgdGhlbgogICAgICAgIGVuYWJsZV9sc19tb2RzZWMKICAgIGVsaWYgWyAiJHtMU1Z9IiA9ICdvcGVubGl0ZXNwZWVkJyBdOyB0aGVuCiAgICAgICAgZW5hYmxlX29sc19tb2RzZWMKICAgIGZpCn0KCmRpc2FibGVfb2xzX21vZGVzZWMoKXsKICAgIGdyZXAgJ21vZHVsZSBtb2Rfc2VjdXJpdHkgeycgJHtPTFNfSFRUUERfQ09ORn0gPi9kZXYvbnVsbCAyPiYxCiAgICBpZiBbICR7P30gLWVxIDAgXSA7IHRoZW4KICAgICAgICBlY2hvICdEaXNhYmxlIG1vZHNlY3VyaXR5JwogICAgICAgIGZzdF9tYXRjaF9saW5lICdtb2R1bGUgbW9kX3NlY3VyaXR5JyAke09MU19IVFRQRF9DT05GfQogICAgICAgIGxzdF9tYXRjaF9saW5lICR7RklSU1RfTElORV9OVU19ICR7T0xTX0hUVFBEX0NPTkZ9ICd9JwogICAgICAgIHNlZCAtaSAiJHtGSVJTVF9MSU5FX05VTX0sJHtMQVNUX0xJTkVfTlVNfWQiICR7T0xTX0hUVFBEX0NPTkZ9CiAgICBlbHNlCiAgICAgICAgZWNobyAnQWxyZWFkeSBkaXNhYmxlZCBmb3IgbW9kc2VjdXJpdHknCiAgICBmaSAgICAKfQoKZGlzYWJsZV9sc19tb2Rlc2VjKCl7CiAgICBncmVwICc8ZW5hYmxlQ2Vuc29yc2hpcD4wPC9lbmFibGVDZW5zb3JzaGlwPicgJHtMU19IVFRQRF9DT05GfQogICAgaWYgWyAkez99IC1lcSAwIF0gOyB0aGVuCiAgICAgICAgZWNobyAnQWxyZWFkeSBkaXNhYmxlZCBmb3IgbW9kc2VjdXJpdHknCiAgICBlbHNlCiAgICAgICAgZWNobyAnRGlzYWJsZSBtb2RzZWN1cml0eScKICAgICAgICBzZWQgLWkgXAogICAgICAgICJzPTxlbmFibGVDZW5zb3JzaGlwPjE8L2VuYWJsZUNlbnNvcnNoaXA+PTxlbmFibGVDZW5zb3JzaGlwPjA8L2VuYWJsZUNlbnNvcnNoaXA+PSIgJHtMU19IVFRQRF9DT05GfQogICAgICAgIGZzdF9tYXRjaF9saW5lICdjZW5zb3JzaGlwUnVsZVNldCcgJHtMU19IVFRQRF9DT05GfQogICAgICAgIGxzdF9tYXRjaF9saW5lICR7RklSU1RfTElORV9OVU19ICR7TFNfSFRUUERfQ09ORn0gJy9jZW5zb3JzaGlwUnVsZVNldCcKICAgICAgICBzZWQgLWkgIiR7RklSU1RfTElORV9OVU19LCR7TEFTVF9MSU5FX05VTX1kIiAke0xTX0hUVFBEX0NPTkZ9CiAgICBmaSAgICAKfQoKZGlzYWJsZV9tb2RzZWMoKXsKICAgIGNoZWNrX2xzdgogICAgaWYgWyAiJHtMU1Z9IiA9ICdsc3dzJyBdOyB0aGVuCiAgICAgICAgZGlzYWJsZV9sc19tb2Rlc2VjCiAgICBlbGlmIFsgIiR7TFNWfSIgPSAnb3BlbmxpdGVzcGVlZCcgXTsgdGhlbgogICAgICAgIGRpc2FibGVfb2xzX21vZGVzZWMKICAgIGZpCn0KCmluc3RhbGxfZ2l0KCl7CiAgICBpZiBbICEgLWYgL3Vzci9iaW4vZ2l0IF07IHRoZW4KICAgICAgICBlY2hvICdJbnN0YWxsIGdpdCcKICAgICAgICBhcHQtZ2V0IGluc3RhbGwgZ2l0IC15ID4vZGV2L251bGwgMj4mMQogICAgZmkKfQoKaW5zdGFsbF9vd2FzcCgpewogICAgY2QgJHtPV0FTUF9ESVJ9CiAgICBlY2hvICdEb3dubG9hZCBPV0FTUCBydWxlcycKICAgIGdpdCBjbG9uZSBodHRwczovL2dpdGh1Yi5jb20vU3BpZGVyTGFicy9vd2FzcC1tb2RzZWN1cml0eS1jcnMuZ2l0ID4vZGV2L251bGwgMj4mMQp9Cgpjb25maWd1cmVfb3dhc3AoKXsKICAgIGVjaG8gJ0NvbmZpZyBPV0FTUCBydWxlcy4nCiAgICBjZCAke09XQVNQX0RJUn0KICAgIGVjaG8gImluY2x1ZGUgbW9kc2VjdXJpdHkuY29uZgppbmNsdWRlIG93YXNwLW1vZHNlY3VyaXR5LWNycy9jcnMtc2V0dXAuY29uZgppbmNsdWRlIG93YXNwLW1vZHNlY3VyaXR5LWNycy9ydWxlcy9SRVFVRVNULTkwMC1FWENMVVNJT04tUlVMRVMtQkVGT1JFLUNSUy5jb25mCmluY2x1ZGUgb3dhc3AtbW9kc2VjdXJpdHktY3JzL3J1bGVzL1JFUVVFU1QtOTAxLUlOSVRJQUxJWkFUSU9OLmNvbmYKaW5jbHVkZSBvd2FzcC1tb2RzZWN1cml0eS1jcnMvcnVsZXMvUkVRVUVTVC05MDMuOTAwMS1EUlVQQUwtRVhDTFVTSU9OLVJVTEVTLmNvbmYKaW5jbHVkZSBvd2FzcC1tb2RzZWN1cml0eS1jcnMvcnVsZXMvUkVRVUVTVC05MDMuOTAwMi1XT1JEUFJFU1MtRVhDTFVTSU9OLVJVTEVTLmNvbmYKaW5jbHVkZSBvd2FzcC1tb2RzZWN1cml0eS1jcnMvcnVsZXMvUkVRVUVTVC05MDMuOTAwMy1ORVhUQ0xPVUQtRVhDTFVTSU9OLVJVTEVTLmNvbmYKaW5jbHVkZSBvd2FzcC1tb2RzZWN1cml0eS1jcnMvcnVsZXMvUkVRVUVTVC05MDMuOTAwNC1ET0tVV0lLSS1FWENMVVNJT04tUlVMRVMuY29uZgppbmNsdWRlIG93YXNwLW1vZHNlY3VyaXR5LWNycy9ydWxlcy9SRVFVRVNULTkwMy45MDA1LUNQQU5FTC1FWENMVVNJT04tUlVMRVMuY29uZgppbmNsdWRlIG93YXNwLW1vZHNlY3VyaXR5LWNycy9ydWxlcy9SRVFVRVNULTkwMy45MDA2LVhFTkZPUk8tRVhDTFVTSU9OLVJVTEVTLmNvbmYKaW5jbHVkZSBvd2FzcC1tb2RzZWN1cml0eS1jcnMvcnVsZXMvUkVRVUVTVC05MDUtQ09NTU9OLUVYQ0VQVElPTlMuY29uZgppbmNsdWRlIG93YXNwLW1vZHNlY3VyaXR5LWNycy9ydWxlcy9SRVFVRVNULTkxMC1JUC1SRVBVVEFUSU9OLmNvbmYKaW5jbHVkZSBvd2FzcC1tb2RzZWN1cml0eS1jcnMvcnVsZXMvUkVRVUVTVC05MTEtTUVUSE9ELUVORk9SQ0VNRU5ULmNvbmYKaW5jbHVkZSBvd2FzcC1tb2RzZWN1cml0eS1jcnMvcnVsZXMvUkVRVUVTVC05MTItRE9TLVBST1RFQ1RJT04uY29uZgppbmNsdWRlIG93YXNwLW1vZHNlY3VyaXR5LWNycy9ydWxlcy9SRVFVRVNULTkxMy1TQ0FOTkVSLURFVEVDVElPTi5jb25mCmluY2x1ZGUgb3dhc3AtbW9kc2VjdXJpdHktY3JzL3J1bGVzL1JFUVVFU1QtOTIwLVBST1RPQ09MLUVORk9SQ0VNRU5ULmNvbmYKaW5jbHVkZSBvd2FzcC1tb2RzZWN1cml0eS1jcnMvcnVsZXMvUkVRVUVTVC05MjEtUFJPVE9DT0wtQVRUQUNLLmNvbmYKaW5jbHVkZSBvd2FzcC1tb2RzZWN1cml0eS1jcnMvcnVsZXMvUkVRVUVTVC05MzAtQVBQTElDQVRJT04tQVRUQUNLLUxGSS5jb25mCmluY2x1ZGUgb3dhc3AtbW9kc2VjdXJpdHktY3JzL3J1bGVzL1JFUVVFU1QtOTMxLUFQUExJQ0FUSU9OLUFUVEFDSy1SRkkuY29uZgppbmNsdWRlIG93YXNwLW1vZHNlY3VyaXR5LWNycy9ydWxlcy9SRVFVRVNULTkzMi1BUFBMSUNBVElPTi1BVFRBQ0stUkNFLmNvbmYKaW5jbHVkZSBvd2FzcC1tb2RzZWN1cml0eS1jcnMvcnVsZXMvUkVRVUVTVC05MzMtQVBQTElDQVRJT04tQVRUQUNLLVBIUC5jb25mCmluY2x1ZGUgb3dhc3AtbW9kc2VjdXJpdHktY3JzL3J1bGVzL1JFUVVFU1QtOTM0LUFQUExJQ0FUSU9OLUFUVEFDSy1OT0RFSlMuY29uZgppbmNsdWRlIG93YXNwLW1vZHNlY3VyaXR5LWNycy9ydWxlcy9SRVFVRVNULTk0MS1BUFBMSUNBVElPTi1BVFRBQ0stWFNTLmNvbmYKaW5jbHVkZSBvd2FzcC1tb2RzZWN1cml0eS1jcnMvcnVsZXMvUkVRVUVTVC05NDItQVBQTElDQVRJT04tQVRUQUNLLVNRTEkuY29uZgppbmNsdWRlIG93YXNwLW1vZHNlY3VyaXR5LWNycy9ydWxlcy9SRVFVRVNULTk0My1BUFBMSUNBVElPTi1BVFRBQ0stU0VTU0lPTi1GSVhBVElPTi5jb25mCmluY2x1ZGUgb3dhc3AtbW9kc2VjdXJpdHktY3JzL3J1bGVzL1JFUVVFU1QtOTQ0LUFQUExJQ0FUSU9OLUFUVEFDSy1KQVZBLmNvbmYKaW5jbHVkZSBvd2FzcC1tb2RzZWN1cml0eS1jcnMvcnVsZXMvUkVRVUVTVC05NDktQkxPQ0tJTkctRVZBTFVBVElPTi5jb25mCmluY2x1ZGUgb3dhc3AtbW9kc2VjdXJpdHktY3JzL3J1bGVzL1JFU1BPTlNFLTk1MC1EQVRBLUxFQUtBR0VTLmNvbmYKaW5jbHVkZSBvd2FzcC1tb2RzZWN1cml0eS1jcnMvcnVsZXMvUkVTUE9OU0UtOTUxLURBVEEtTEVBS0FHRVMtU1FMLmNvbmYKaW5jbHVkZSBvd2FzcC1tb2RzZWN1cml0eS1jcnMvcnVsZXMvUkVTUE9OU0UtOTUyLURBVEEtTEVBS0FHRVMtSkFWQS5jb25mCmluY2x1ZGUgb3dhc3AtbW9kc2VjdXJpdHktY3JzL3J1bGVzL1JFU1BPTlNFLTk1My1EQVRBLUxFQUtBR0VTLVBIUC5jb25mCmluY2x1ZGUgb3dhc3AtbW9kc2VjdXJpdHktY3JzL3J1bGVzL1JFU1BPTlNFLTk1NC1EQVRBLUxFQUtBR0VTLUlJUy5jb25mCmluY2x1ZGUgb3dhc3AtbW9kc2VjdXJpdHktY3JzL3J1bGVzL1JFU1BPTlNFLTk1OS1CTE9DS0lORy1FVkFMVUFUSU9OLmNvbmYKaW5jbHVkZSBvd2FzcC1tb2RzZWN1cml0eS1jcnMvcnVsZXMvUkVTUE9OU0UtOTgwLUNPUlJFTEFUSU9OLmNvbmYKaW5jbHVkZSBvd2FzcC1tb2RzZWN1cml0eS1jcnMvcnVsZXMvUkVTUE9OU0UtOTk5LUVYQ0xVU0lPTi1SVUxFUy1BRlRFUi1DUlMuY29uZiI+bW9kc2VjX2luY2x1ZGVzLmNvbmYKICAgIGVjaG8gIlNlY1J1bGVFbmdpbmUgT24iPm1vZHNlY3VyaXR5LmNvbmYKICAgIGNkICR7T1dBU1BfRElSfS9vd2FzcC1tb2RzZWN1cml0eS1jcnMKICAgIGlmIFsgLWYgY3JzLXNldHVwLmNvbmYuZXhhbXBsZSBdOyB0aGVuCiAgICAgICAgbXYgY3JzLXNldHVwLmNvbmYuZXhhbXBsZSBjcnMtc2V0dXAuY29uZgogICAgZmkgICAgCiAgICBjZCAke09XQVNQX0RJUn0vb3dhc3AtbW9kc2VjdXJpdHktY3JzL3J1bGVzCiAgICBpZiBbIC1mIFJFUVVFU1QtOTAwLUVYQ0xVU0lPTi1SVUxFUy1CRUZPUkUtQ1JTLmNvbmYuZXhhbXBsZSBdOyB0aGVuCiAgICAgICAgbXYgUkVRVUVTVC05MDAtRVhDTFVTSU9OLVJVTEVTLUJFRk9SRS1DUlMuY29uZi5leGFtcGxlIFJFUVVFU1QtOTAwLUVYQ0xVU0lPTi1SVUxFUy1CRUZPUkUtQ1JTLmNvbmYKICAgIGZpCiAgICBpZiBbIC1mIFJFU1BPTlNFLTk5OS1FWENMVVNJT04tUlVMRVMtQUZURVItQ1JTLmNvbmYuZXhhbXBsZSBdOyB0aGVuCiAgICAgICAgbXYgUkVTUE9OU0UtOTk5LUVYQ0xVU0lPTi1SVUxFUy1BRlRFUi1DUlMuY29uZi5leGFtcGxlIFJFU1BPTlNFLTk5OS1FWENMVVNJT04tUlVMRVMtQUZURVItQ1JTLmNvbmYKICAgIGZpCn0KCm1haW5fb3dhc3AoKXsKICAgIG1rX293YXNwX2RpcgogICAgaW5zdGFsbF9naXQKICAgIGluc3RhbGxfb3dhc3AKICAgIGNvbmZpZ3VyZV9vd2FzcAogICAgY2hlY2tfbHN2CiAgICBlbmFibGVfbW9kc2VjICAgIAp9CgpjaGVja19pbnB1dCAkezF9CndoaWxlIFsgISAteiAiJHsxfSIgXTsgZG8KICAgIGNhc2UgJHsxfSBpbgogICAgICAgIC1baEhdIHwgLWhlbHAgfCAtLWhlbHApCiAgICAgICAgICAgIGhlbHBfbWVzc2FnZQogICAgICAgICAgICA7OwogICAgICAgIC1bZUVdIHwgLWVuYWJsZSB8IC0tZW5hYmxlKQogICAgICAgICAgICBtYWluX293YXNwCiAgICAgICAgICAgIDs7CiAgICAgICAgLVtkRF0gfCAtZGlzYWJsZSB8IC0tZGlzYWJsZSkKICAgICAgICAgICAgZGlzYWJsZV9tb2RzZWMKICAgICAgICAgICAgOzsgICAgICAgICAgCiAgICAgICAgKikgCiAgICAgICAgICAgIGhlbHBfbWVzc2FnZQogICAgICAgICAgICA7OwogICAgZXNhYwogICAgc2hpZnQKZG9uZQ=="
                    },

                    new File
                    {
                        path= "bin/container/",
                        filename="serialctl.sh",
                        content= "IyEvYmluL2Jhc2gKTFNESVI9Jy91c3IvbG9jYWwvbHN3cycKRVBBQ0U9JyAgICAgICAgJwoKZWNob3coKXsKICAgIEZMQUc9JHsxfQogICAgc2hpZnQKICAgIGVjaG8gLWUgIlwwMzNbMW0ke0VQQUNFfSR7RkxBR31cMDMzWzBtJHtAfSIKfQoKaGVscF9tZXNzYWdlKCl7CiAgICBlY2hvIC1lICJcMDMzWzFtT1BUSU9OU1wwMzNbMG0iCiAgICBlY2hvdyAnLVMsIC0tc2VyaWFsIFtZT1VSX1NFUklBTHxUUklBTF0nCiAgICBlY2hvICIke0VQQUNFfSR7RVBBQ0V9V2lsbCBhcHBseSBhbmQgcmVnaXN0ZXIgdGhlIHNlcmlhbCB0byBMU1dTLiIKICAgIGVjaG93ICctSCwgLS1oZWxwJwogICAgZWNobyAiJHtFUEFDRX0ke0VQQUNFfURpc3BsYXkgaGVscCBhbmQgZXhpdC4iICAgICAgIAogICAgZXhpdCAwCn0KCmNoZWNrX2lucHV0KCl7CiAgICBpZiBbIC16ICIkezF9IiBdOyB0aGVuCiAgICAgICAgaGVscF9tZXNzYWdlCiAgICAgICAgZXhpdCAxCiAgICBmaQp9CgpiYWNrdXBfb2xkKCl7CiAgICBpZiBbIC1mICR7MX0gXSAmJiBbICEgLWYgJHsxfV9vbGQgXTsgdGhlbgogICAgICAgbXYgJHsxfSAkezF9X29sZAogICAgZmkKfQoKZGV0ZWN0X29scygpewogICAgaWYgWyAtZSAke0xTRElSfS9iaW4vb3BlbmxpdGVzcGVlZCBdOyB0aGVuCiAgICAgICAgZWNobyAnW1hdIERldGVjdCBPcGVuTGl0ZVNwZWVkLCBhYm9ydCEnCiAgICAgICAgZXhpdCAxCiAgICBmaSAgICAKfQoKYXBwbHlfc2VyaWFsKCl7CiAgICBkZXRlY3Rfb2xzCiAgICBjaGVja19pbnB1dCAkezF9CiAgICBlY2hvICR7MX0gfCBncmVwIC1pICd0cmlhbCcgPi9kZXYvbnVsbAogICAgaWYgWyAkez99ID0gMCBdOyB0aGVuIAogICAgICAgIGVjaG8gJ0FwcGx5IFRyaWFsIExpY2Vuc2UnCiAgICAgICAgaWYgWyAhIC1lICR7TFNESVJ9L2NvbmYvc2VyaWFsLm5vIF0gJiYgWyAhIC1lICR7TFNESVJ9L2NvbmYvbGljZW5zZS5rZXkgXTsgdGhlbgogICAgICAgICAgICBybSAtZiAke0xTRElSfS9jb25mL3RyaWFsLmtleSoKICAgICAgICAgICAgd2dldCAtUCAke0xTRElSfS9jb25mIC1xIGh0dHA6Ly9saWNlbnNlLmxpdGVzcGVlZHRlY2guY29tL3Jlc2VsbGVyL3RyaWFsLmtleQogICAgICAgICAgICBlY2hvICdBcHBseSB0cmlhbCBmaW5pc2hlZCcKICAgICAgICBlbHNlCiAgICAgICAgICAgIGVjaG8gIlBsZWFzZSBiYWNrdXAgYW5kIHJlbW92ZSB5b3VyIGV4aXN0aW5nIGxpY2Vuc2UsIGFwcGx5IGFib3J0ISIKICAgICAgICAgICAgZXhpdCAxICAgIAogICAgICAgIGZpCiAgICBlbHNlCiAgICAgICAgZWNobyAiQXBwbHkgU2VyaWFsIG51bWJlcjogJHsxfSIKICAgICAgICBiYWNrdXBfb2xkICR7TFNESVJ9L2NvbmYvc2VyaWFsLm5vCiAgICAgICAgYmFja3VwX29sZCAke0xTRElSfS9jb25mL2xpY2Vuc2Uua2V5CiAgICAgICAgYmFja3VwX29sZCAke0xTRElSfS9jb25mL3RyaWFsLmtleQogICAgICAgIGVjaG8gIiR7MX0iID4gJHtMU0RJUn0vY29uZi9zZXJpYWwubm8KICAgICAgICAke0xTRElSfS9iaW4vbHNodHRwZCAtcgogICAgICAgIGlmIFsgLWYgJHtMU0RJUn0vY29uZi9saWNlbnNlLmtleSBdOyB0aGVuCiAgICAgICAgICAgIGVjaG8gJ1tPXSBBcHBseSBzdWNjZXNzJwogICAgICAgIGVsc2UgCiAgICAgICAgICAgIGVjaG8gJ1tYXSBBcHBseSBmYWlsZWQsIHBsZWFzZSBjaGVjayEnCiAgICAgICAgICAgIGV4aXQgMQogICAgICAgIGZpCiAgICBmaQp9CgpjaGVja19pbnB1dCAkezF9CndoaWxlIFsgISAteiAiJHsxfSIgXTsgZG8KICAgIGNhc2UgJHsxfSBpbgogICAgICAgIC1baEhdIHwgLWhlbHAgfCAtLWhlbHApCiAgICAgICAgICAgIGhlbHBfbWVzc2FnZQogICAgICAgICAgICA7OwogICAgICAgIC1bc1NdIHwgLXNlcmlhbCB8IC0tc2VyaWFsKSBzaGlmdAogICAgICAgICAgICBhcHBseV9zZXJpYWwgIiR7MX0iCiAgICAgICAgICAgIDs7ICAgICAgICAgICAgCiAgICAgICAgKikKICAgICAgICAgICAgaGVscF9tZXNzYWdlCiAgICAgICAgICAgIDs7CiAgICBlc2FjCiAgICBzaGlmdApkb25l"
                    }
                         }
                    };

                    string result = JsonConvert.SerializeObject(dappDefinitionModel);
                    Console.WriteLine(result);
                    // Read Json File into dappDefinitionModel

                     
                    await _dappProvisioner.ProvisionNewAppAsync(dappDefinitionModel, deploymentModel);
 
                }
                catch (Exception e)
                {
                    _logger.LogInformation($"Creating app zone '{appname}'failed");
                    _logger.LogInformation(e.Message);

                }

                if (zoneCreated)
                {

                    MoveDocument(document, xDocumentPendingCollection);

                }

            }
        }

        private void MoveDocument(BsonDocument document, IMongoCollection<BsonDocument> xDocumentPendingCollection)
        {
            xDocumentPendingCollection.DeleteOne(document);

            

            var xDocumentCollection = _db.GetCollection<BsonDocument>("xDocument");
            xDocumentCollection.InsertOne(document);
        }




        private async Task<string> GetMyFeeAddress()
        {



            var client = new RestClient(_xServerHost);
            var request = new RestRequest($"status", Method.Get);
            request.AddHeader("content-type", "application/json");


            var response = await client.ExecuteAsync<StatusResult>(request);

            return response.Data.FeeAddress;
        }




        private static void ValidateModel(object app)
        {
            var context = new ValidationContext(app, serviceProvider: null, items: null);
            var validationResults = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(app, context, validationResults, true);

            if (!isValid)
            {

                throw new Exception(validationResults.FirstOrDefault().ErrorMessage);

            }
        }

        private async Task<int> GetBlockCount()
        {
            var request = new RestRequest($"BlockStore/getblockcount");
            var result = await _restClient.GetAsync<int>(request);

            return result;
        }

        private async Task<string> GetBlockHash(int height)
        {


            var request = new RestRequest($"Consensus/getblockhash?height={height}");
            var result = await _restClient.GetAsync<string>(request);

            if (result != null) { return result; }

            var errorMessage = $"Block at height {height} not found";
            _logger.LogError(errorMessage);

            return errorMessage;


        }

        private async Task<BlockModel> GetBlock(string blockHash)
        {

            var request = new RestRequest($"BlockStore/block?Hash={blockHash}&ShowTransactionDetails=true&OutputJson=true");

            var result = await _restClient.GetAsync<BlockModel>(request);
            if (result != null) { return result; }

            var errorMessage = $"Block hash {blockHash} not found";
            _logger.LogError(errorMessage);

            return null;
        }



        public static byte[] FromHex(string hex)
        {
            hex = hex.Replace("-", "");
            byte[] raw = new byte[hex.Length / 2];
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return raw;
        }

        private decimal GetFee(InstructionTypeEnum instructionTypeEnum)
        {

            switch (instructionTypeEnum)
            {
                case InstructionTypeEnum.NewDnsZone:
                    return 2;

                case InstructionTypeEnum.UpdateDnsZone:
                    return 0.5m;

                default:
                    return 0;

            }


        }
    }
}
