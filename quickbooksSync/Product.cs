using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Zombie;
using Interop.QBFC13;

namespace SkarAudioQBSync
{
    //Saving products in a simple object
    class Product : QBSync
    {
        public string LocationName = "testLocation2";
        private Decimal price;
        private string name;
        private string sku;
        private Decimal nonInventoryItem = 0;
        private Decimal quantity = 1;
        public Product(Zombie.SDKConnection cn) : base(cn)
        {
           
        }
        public void setNonInventoryItem(Decimal nonInventoryItem)
        {
            this.nonInventoryItem = nonInventoryItem;
        }
        public Decimal getIsNonInventoryItem()
        {
            return this.nonInventoryItem;
        }
        public void setSku(string sku)
        {
            this.sku = sku;
        }
        public string getSku()
        {
            return this.sku;
        }

        public void setName(string name)
        {
            this.name = name;
        }
        public string getName()
        {
            return this.name;
        }
        public void setListID(string listId)
        {
            this.ListID = listId;
        }
        

        public void load(string productId)
        {

            var batch = this.cn.NewBatch();

            var productQry = batch.MsgSet.AppendItemInventoryQueryRq();
            productQry.ORListQueryWithOwnerIDAndClass.FullNameList.Add(productId);

            batch.SetClosures(productQry, b =>
            {
                var products = new QBFCIterator<IItemInventoryRetList, IItemInventoryRet>(b);

                if (products.Count() == 1)
                {
                    this.sku = productId;
                    this.loaded = true;
                    this.ListID = Safe.Value(products.First().ListID);
                    this.FullName = Safe.Value(products.First().FullName);
                }
            });

            batch.Run();
        }
        public void setLoaded(Boolean isLoaded)
        {
            this.loaded = isLoaded;
        }
        public void setLocationName(String locationId)
        {
            if(locationId == "545712")
            {
                this.LocationName = "Las Vegas WH";
            }
            else
            {
                this.LocationName = "Tampa WH 1";
            }
        }
        public void setPrice(Decimal price)
        {
            this.price = price;
        }

        public Decimal getPrice()
        {
            return this.price;
        }
        public void setQuantity(Decimal quant)
        {
            this.quantity = quant;
        }
        public Decimal getQuantity()
        {
            return this.quantity;
        }
    }
}
