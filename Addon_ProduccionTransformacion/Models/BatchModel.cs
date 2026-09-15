using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Addon_TransformProduction.Models
{
    public class BatchModel
    {
        public string ItemCode { get; set; }
        public string BatchNumber { get; set; }
        public DateTime ExpDate { get; set; } 
        public string Warehouse { get; set; }
        public string UnitMeasurement { get; set; }
        public double QuantityAvailable { get; set; }
        public double QuantityAssigned { get; set; }
        public decimal PricePurLast { get; set; }
        public decimal Price { get; set; }
    }
}
