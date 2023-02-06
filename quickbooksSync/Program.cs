using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using MySql.Data.Types;
using Zombie;
using Interop.QBFC13;
using Renci.SshNet;
namespace SkarAudioQBSync
{
    class Program
    {
        private bool isDebug = true;
        private SalesReceipt sale = null;
        private int orderCount = 50;
        private String orderNumber = "";
        private bool dryRun = false;
        private List<string> commandList = new List<string>(){ "help  | this command :)", "orders (ordercount) | tell me how many orders I should import", "dryrun (ordercount) | No data will be saved, only for testing" };
        private string command = "orders";
        private RemoteConnection remote;
        static void Main(string[] args)
        {
            Program prog = new Program();
            prog.run(args);

        }
        public void log(String text, bool newLine = true)
        {
            if(isDebug)
            {
                if (newLine)
                {
                    remote.LogTable(text);
                    Console.WriteLine(text);
                }
                else
                {
                    Console.Write(text);
                }
                
            }
            else
            {
                //Maybe log to event log?
                remote.LogTable(text);
            }
        }
        public void listCommands()
        {
            Console.WriteLine("Available Commands:");
            foreach (string val in commandList)
            {
                Console.WriteLine(val);
            }
            Console.ReadLine();
        }
        public bool run(string[] args)
        {
            try
            {

                if (args.Length > 0)
                {
                    if (args[0] == "help")
                    {
                        this.listCommands();

                        return false;
                    }
                    if (args[0] == "orders")
                    {
                        this.orderCount = int.Parse(args[1]);
                    }
                    if (args[0] == "dryrun")
                    {
                        this.dryRun = true;
                        this.orderCount = int.Parse(args[1]);
                    }
                    if(args[0] == "products")
                    {
                        this.command = "products";
                    }
                    if(args[0] == "orderNumber")
                    {
                        this.orderNumber = args[1];
                        this.command = "orderNumber";
                        this.orderCount = 1;
                    }
                }

                ConnectionMgr.InitDesktop("Skar QBInterface");
                
                //the StatusConsole class will direct all error and trace information to the console
                StatusMgr.AddListener(new StatusConsole(), true);


                //Connect to laravel mysql database through ssh tunnel
                this.remote = new RemoteConnection();
                bool tunnelStarted = remote.createTunnel();

                if (tunnelStarted == false)
                {
                    this.log("Error Creating Tunnel");
                    return false;
                }


                Settings appSettings = remote.getSettings();
                if(appSettings.getEnabled() == "0")
                {
                    this.log("app disabled, do nothing");

                    return false;
                }


                if (this.command == "orders")
                {
                    Console.WriteLine("lets sync " + this.orderCount);
                    if (this.dryRun)
                        Console.WriteLine("this is a dryrun");

                    //Get new orders we need to import
                    this.log("getting orders with an order date of equal or higher to: " + appSettings.getMinOrderDate());
                    DataTable orderList = remote.getImportableOrders(orderCount, appSettings.getMinOrderDate());
                    if (orderList.Rows.Count <= 0)
                    {
                        this.log("no orders found");
                        return false;
                    }
                    this.log("orders fetched");

                    this.importNewOrders(orderList, remote);
                }else if(this.command == "products")
                {
                    //We have everything we need, lets start our connection to QB
                    using (var cn = ConnectionMgr.GetConnection())
                    {
                        remote.connecMysql();
                        Console.WriteLine("synchronize bundle products");
                        BundleProducts products = new BundleProducts(cn, remote);
                        products.load();
                    }
                }else if(this.command == "orderNumber")
                {
                    DataTable orderList = remote.getImportableOrders(orderCount, appSettings.getMinOrderDate(), this.orderNumber);
                    if (orderList.Rows.Count <= 0)
                    {
                        this.log("no orders found");
                        return false;
                    }
                    this.log("orders fetched");

                    this.importNewOrders(orderList, remote);
                }
               
                
                    remote.closeTunnel();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
          
            return true;
        }
          
        private void saveSalesReceipt(SalesReceipt sale, RemoteConnection remote)
        {
            if (sale.createSalesReceipt())
            {
                //Sales receipt created
                remote.setImported(sale.getLaravelOrderId());

                this.log("+ <a href='/nova/resources/orders/" + sale.getLaravelOrderId() + "'>" + sale.getRemoteOrderId() + "</a> : " + sale.getCustomer().getFullName());
            }
            else
            {
                this.log("- <a href='/nova/resources/orders/" + sale.getLaravelOrderId() + "'>" + sale.getRemoteOrderId() + "</a> : " + sale.getCustomer().getFullName());

                //Something went wrong, lets let our webinterface know
            }
        }
        private void importNewOrders(DataTable orderList, RemoteConnection remote)
        {
            try
            {
                //We have everything we need, lets start our connection to QB
                using (var cn = ConnectionMgr.GetConnection())
                {
                    string prevOrderId = "";
                    String logString = "";
                    Boolean orderProblems = false;
                    string currentBundleSku = "";
                    int orderListCount = orderList.Rows.Count;
                    int i = 0;
                    foreach (DataRow row in orderList.Rows)
                    {
                        i++;
                        //If this is the first loop, or a new orderId
                        if (prevOrderId == "" || prevOrderId != row["id"].ToString())
                        {
                            //Check if the sale object is ready for saving (if a previous order was looped through)
                            if (sale != null && sale.lineItemCount() > 0 && orderProblems == false)
                            {
                                if (this.dryRun == false)
                                {

                                    this.saveSalesReceipt(sale, remote);
                                    
                                }
                                else
                                {
                                    this.log("Order created (dryrun)");
                                }
                            }
                            else
                            {
                                if (orderProblems == true)
                                {
                                    remote.setError(sale.getLaravelOrderId(), logString);
                                    Console.WriteLine("- " + row["id"].ToString(), false);
                                }
                                //Lets update webinterface, no lineitems created
                                Console.WriteLine("+*+", false);
                            }

                            logString = "";
                            orderProblems = false;
                            //Start new sales receipt, Try and load customer
                            var customer = new Customer(cn);
                            customer.load(row);

                            prevOrderId = row["id"].ToString();
                            sale = new SalesReceipt(cn);
                            sale.setLaravelOrderId(row["id"].ToString());
                            sale.setRemoteOrderId(row["orderNumber"].ToString());
                            sale.setOrderDate(DateTime.Parse(row["orderDate"].ToString()));
                            sale.setOrderTotal(Decimal.Parse(row["orderTotal"].ToString()));
                            sale.setPaymentMethod(row["paymentMethod"].ToString());
                            sale.setShippingAmount(Decimal.Parse(row["shippingAmount"].ToString()));
                            sale.setTaxAmount(Decimal.Parse(row["taxAmount"].ToString()));
                            sale.setCustomer(customer);

                        }

                        
                        var prod = new Product(cn);
                        Console.WriteLine("non inventory?");
                        Console.WriteLine(row["nonInventoryItem"].ToString());

                      if (row["isBundle"].ToString() == "1")
                        {
                            prod.load(row["sku"].ToString());

                            if (currentBundleSku == "" || currentBundleSku != row["bundleSku"].ToString())
                            {
                                this.log("Create bundle for " + row["bundleSku"].ToString());
                                var bundleProduct = new Product(cn);
                                bundleProduct.load("Bundle Revenue");
                                bundleProduct.setQuantity(1);
                                
                                bundleProduct.setPrice(Decimal.Parse(row["unitPrice"].ToString()) * Decimal.Parse(row["quantity"].ToString()));
                                bundleProduct.setName("Bundle Revenue");

                                if (prod.getLoaded())
                                {
                                    this.sale.addLineItem(bundleProduct);
                                }
                                else
                                {
                                    Console.WriteLine("bundle prod not found, order: " + sale.getLaravelOrderId());
                                }
                                currentBundleSku = row["bundleSku"].ToString();
                               
                            }
                          
                            prod.setPrice(0);
                            prod.setQuantity(Decimal.Parse(row["quantity"].ToString()));
                            prod.setName(row["name"].ToString());
                            prod.setLocationName(row["warehouseId"].ToString());

                        }
                        else if (row["nonInventoryItem"].ToString() == "0")
                        {
                            prod.load(row["sku"].ToString());
                            prod.setPrice(Decimal.Parse(row["unitPrice"].ToString()) * Decimal.Parse(row["quantity"].ToString()));
                            prod.setQuantity(Decimal.Parse(row["quantity"].ToString()));
                            prod.setName(row["name"].ToString());
                            prod.setLocationName(row["warehouseId"].ToString());
                        }
                        else if (row["nonInventoryItem"].ToString() == "1")
                        {
                            this.log("adding non inventory item " + row["name"].ToString());
                            prod.setSku(row["sku"].ToString());
                            prod.setFullName(row["sku"].ToString());
                            prod.setListID(row["sku"].ToString());
                            prod.setLoaded(true);
                            prod.setNonInventoryItem(1);
                            prod.setPrice(Decimal.Parse(row["unitPrice"].ToString()) * Decimal.Parse(row["quantity"].ToString()));
                            prod.setQuantity(Decimal.Parse(row["quantity"].ToString()));
                            prod.setName("Automatic Discount");
                        }
                        if (prod.getLoaded())
                        {
                            this.sale.addLineItem(prod);
                        }
                        else
                        {
                            ///Update logstring, products not found in qb database
                            this.log("product not found in qb " + row["sku"].ToString() + " for order " + sale.getLaravelOrderId());
                            orderProblems = true;
                            logString += "product not found in qb " + row["sku"].ToString() + "\n";
                        }

                        if((orderNumber != "" && orderListCount == i))
                        {
                            if (orderProblems == false)
                            {
                                Console.WriteLine("save single order");
                                this.saveSalesReceipt(sale, remote);
                            }
                            else
                            {
                                remote.setError(sale.getLaravelOrderId(), logString);
                            }
                        }
                    }
                }
            }catch(Exception e)
            {
                Console.WriteLine("nope");
            }
        }
    }
}
