using System;

namespace Executive_Fuentes.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Role { get; set; }

        public string Action { get; set; }
        public string Module { get; set; }
        public string Description { get; set; }

        public DateTime DateTime { get; set; }

        public string IPAddress { get; set; }
    }
}