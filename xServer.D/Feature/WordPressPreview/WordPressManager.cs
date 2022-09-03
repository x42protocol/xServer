using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using x42.Controllers.Requests;
using x42.Feature.Database;
using x42.Feature.Database.Context;
using x42.Feature.Database.Tables;
using x42.Feature.Network;
using x42.Feature.PowerDns;
using x42.Feature.Profile;
using x42.Feature.WordPressPreview.Models;

namespace x42.Feature.WordPressPreview
{
    public partial class WordPressManager
    {

        private readonly PowerDnsFeature _powerDnsFeature;
        private readonly NetworkFeatures _networkFeatures;
        private readonly DatabaseSettings _databaseSettings;

        public WordPressManager(PowerDnsFeature powerDnsFeature, NetworkFeatures networkFeatures, DatabaseSettings databaseSettings)
        {
            _powerDnsFeature = powerDnsFeature;
            _networkFeatures = networkFeatures;
            _databaseSettings = databaseSettings;
        }

        public async Task<List<string>> GetWordPressPreviewDomains()
        {
            try
            {
            
                var result = new List<string>();

                for (int i = 0; i < 5; i++)
                {
                    Thread.Sleep(10);

                    result.Add(RandName());
                }
                return await Task.FromResult(result);
            
            }
            catch (Exception ex)
            {
 
                return null;
            }
        }

  

        public async Task<WordpressDomainResult> ReserveWordpressPreviewDNS(WordPressReserveRequest registerRequest)
        {
            WordpressDomainResult result = new WordpressDomainResult();


            using (X42DbContext dbContext = new X42DbContext(_databaseSettings.ConnectionString))
            {
                var hasEntry = dbContext.Domains.Where(l => l.Name == registerRequest.Name).Count() > 0;

                if (hasEntry) {

                    result.Success = false;
                    result.ResultMessage = "Domain Already Registered.";
                    return result;

                }
            }


            bool isRegisterKeyValid = await _networkFeatures.IsRegisterKeyValid(registerRequest.Name, registerRequest.KeyAddress, registerRequest.ReturnAddress, registerRequest.Signature);
            if (!isRegisterKeyValid)
            {
                result.Success = false;
                result.ResultMessage = "Signature validation failed.";
                return result;
            }

            // Price Lock ID does not exist, this is a new request, so let's create a price lock ID for it, and reserve the name.
            var profilePriceLockRequest = new CreatePriceLockRequest()
            {
                DestinationAddress = _networkFeatures.GetMyFeeAddress(),
                RequestAmount = 0.5m,      // $0.50
                RequestAmountPair = 1,  // USD
                ExpireBlock = 15
            };
            var newPriceLock = await _networkFeatures.CreateNewPriceLock(profilePriceLockRequest);
            if (newPriceLock == null || string.IsNullOrEmpty(newPriceLock?.PriceLockId) || newPriceLock?.ExpireBlock <= 0)
            {
                result.Success = false;
                result.ResultMessage = "Failed to acquire a price lock";
                return result;
            }

            int status = (int)Status.Reserved;
            var newProfile = new DomainData()
            {
                Name = registerRequest.Name,
                KeyAddress = registerRequest.KeyAddress,
                ReturnAddress = registerRequest.ReturnAddress,
                PriceLockId = newPriceLock.PriceLockId,
                Signature = registerRequest.Signature,
                Status = status,
                Relayed = false
            };
            using (X42DbContext dbContext = new X42DbContext(_databaseSettings.ConnectionString))
            {
                var newRecord = dbContext.Domains.Add(newProfile);
                if (newRecord.State == EntityState.Added)
                {
                    dbContext.SaveChanges();
                    result.PriceLockId = newPriceLock.PriceLockId;
                    result.Status = status;
                    result.Success = true;
                }
                else
                {
                    result.Status = (int)Status.Rejected;
                    result.ResultMessage = "Failed to add domain.";
                    result.Success = false;
                }
            }

            try
            {
                await _powerDnsFeature.AddNewSubDomain(registerRequest.Name);

            }
            catch (Exception)
            {

                result.Success = false;
                result.ResultMessage = "Failed to register subdomain";
                return result;
            }



            return result;
        }

        private string RandName()
        {
            Random rand = new Random(DateTime.Now.Millisecond);
            string[] color = new string[] { "blue", "red", "yellow", "orange", "green", "pink", "black", "white", "violet", "magenta", "brow", "golden" };
            string[] emotion = new string[] { "happy", "sad", "excited", "angry", "nervous", "amused", "anxious", "awkward", "calm", "confused", "joyful", "romantic", "surprised" };
            string[] animal = new string[] { "fox", "rabbit", "cow", "dog", "cat", "horse", "tiger", "lion", "shark", "wolf", "bear", "koala", "panda", "giraffe", "turtle", "deer", "cheetah" };

            var name = emotion[rand.Next(0, emotion.Length - 1)] + "-" + color[rand.Next(0, color.Length - 1)] + "-" + animal[rand.Next(0, animal.Length - 1)] + "";


            return name;

        }
    }
}
