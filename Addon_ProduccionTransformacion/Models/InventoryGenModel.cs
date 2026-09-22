using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Addon_TransformProduction.Models
{
    public class InventoryGenModel
    {
        public DateTime? DocDate { get; set; }
        public DateTime? TaxDate { get; set; }

        public List<Item> Items { get; set; } = new List<Item>();
        

        public class Item
        {
            public string ItemCode { get; set; }
            public string Warehouse { get; set; }
            public string UnitMeasurement { get; set; }
            public double Quantity { get; set; }
            public decimal Price { get; set; }
            public string AcctCode { get; set; }

            public List<Batch> Batches { get; set; } = new List<Batch>();

            public class Batch
            {

                public string BatchNumber { get; set; }
                public DateTime ExpDate { get; set; }
                public DateTime MnfDate { get; set; }
                public DateTime InDate { get; set; }
                public double Quantity { get; set; }
                
            }

            internal void AddBatch(Batch batch)
            {
                if (batch != null)
                {
                    Batches.Add(batch);
                }
            }


        }

        internal void ClearData()
        {
            DocDate = null;
            TaxDate = null;
            Items.Clear();
        }



    } 

    
}
