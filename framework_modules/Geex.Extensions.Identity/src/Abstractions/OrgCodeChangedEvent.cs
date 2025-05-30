﻿namespace Geex.Extensions.Identity
{
    public class OrgCodeChangedEvent : MediatR.INotification
    {

        public OrgCodeChangedEvent(string oldOrgCode, string newOrgCode)
        {
            this.NewOrgCode = newOrgCode;
            this.OldOrgCode = oldOrgCode;
        }
        public string OldOrgCode { get; set; }
        public string NewOrgCode { get; set; }
    }
}
