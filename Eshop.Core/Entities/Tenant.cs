using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eshop.Core.Entities
{
    public class Tenant
    {
        public string Id { get; set; } = string.Empty; // π.χ. "nicks-shoes"
        public string Name { get; set; } = string.Empty; // π.χ. "Nick's Shoe Store"
        public string ConnectionString { get; set; } = string.Empty; // Η βάση του
        public bool IsActive { get; set; } = true;
    }
}
