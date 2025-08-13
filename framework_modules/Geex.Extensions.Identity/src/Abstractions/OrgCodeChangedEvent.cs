namespace Geex.Extensions.Identity
{
    public class OrgCodeChangedEvent : MediatX.IEvent
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
