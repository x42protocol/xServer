﻿
using Common.Enums;

namespace Common.Models.XDocuments
{
    public abstract class XDocumentModel
    {
        public XDocumentTypeEnum DocumentType { get; set; }
        public InstructionTypeEnum ActionType { get; set; }
        public string KeyAddress { get; set; }
        public string Signature { get; set; }

    }
}
