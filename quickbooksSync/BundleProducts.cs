using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using MySql.Data.Types;
using MySql.Data.Common;
using System.Data;
using System.Configuration;

using Zombie;
using Interop.QBFC13;

namespace SkarAudioQBSync
{
    class BundleProducts : QBSync
    {
        private List<string> skus = new List<string>();
        private RemoteConnection mysqlDb;
        public BundleProducts(Zombie.SDKConnection cn, RemoteConnection db) : base(cn)
        {
            this.mysqlDb = db;
            this.skus = getChildProducts();
        }

        public List<string> getChildProducts()
        {
            this.mysqlDb.getConnection().Open();
                MySqlCommand query = new MySqlCommand("select sku from products", this.mysqlDb.getConnection());

                MySqlDataReader dataReader = query.ExecuteReader();
                DataTable dataTable = new DataTable();
                dataTable.BeginLoadData();
                dataTable.Load(dataReader);
                dataTable.EndLoadData();

                dataReader.Close();

                

                List<string> skus = new List<string>();
                foreach (DataRow row in dataTable.Rows)
                {
                    skus.Add(row["sku"].ToString());

                }
            this.mysqlDb.getConnection().Close();
            return skus;
        }


        public void insertProduct(string sku, string name)
        {

            MySqlCommand query = new MySqlCommand("insert into products (sku, description) VALUES (?sku,?description)", this.mysqlDb.getConnection());
            query.Parameters.AddWithValue("?sku", sku);
            query.Parameters.AddWithValue("?description", name);
            
            query.ExecuteNonQuery();
            
        }

        private void insertNewProducts(List<Product> products)
        {
            this.mysqlDb.getConnection().Open();
            foreach (Product product in products)
            {
                insertProduct(product.getSku(), product.getName());
            }
            this.mysqlDb.getConnection().Close();
        }

        //Load all products from quicbooks, with a custom field
        public void load()
        {
            try
            {
                var batch = this.cn.NewBatch();

                var productQry = batch.MsgSet.AppendItemInventoryQueryRq();
                productQry.IncludeRetElementList.Add("FullName");
                productQry.IncludeRetElementList.Add("Name");
                productQry.IncludeRetElementList.Add("DataExtRet");
                productQry.OwnerIDList.Add("0");

                batch.SetClosures(productQry, b =>
                {
                    var products = new QBFCIterator<IItemInventoryRetList, IItemInventoryRet>(b);

                    List<Product> productsToInsert = new List<Product>();
                    if (products.Count() > 0)
                    {
                        foreach (var product in products)
                        {
                            
                            if (product.DataExtRetList != null)
                            {
                                for (int x = 0; x < product.DataExtRetList.Count; x++)
                                {
                                    IDataExtRet dataExt = product.DataExtRetList.GetAt(x);   
                                    if (Safe.Value(dataExt.DataExtName).ToLower() == "childitem" && Safe.Value(dataExt.DataExtValue).ToLower() == "yes")
                                    {
                                     var test = this.skus.FirstOrDefault(prodSku => prodSku.Contains(product.Name.GetValue()));
                                        
                                        if (test == null)
                                        {
                                            Console.Write('+');
                                            Product prod = new Product(cn);
                                            prod.setSku(product.Name.GetValue());
                                            prod.setName(product.FullName.GetValue());
                                            productsToInsert.Add(prod);
                                        }
                                        else
                                        {

                                            Console.Write('-');
                                        }
                                    }
                                }
                            }
                        }
                        this.insertNewProducts(productsToInsert);

                        this.mysqlDb.getConnection().Close();
                    }
                });


                batch.Run();
            }catch(Exception e)
            {
                Console.WriteLine("test");
            }
        }
    }
}
