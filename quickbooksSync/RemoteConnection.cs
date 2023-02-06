using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;
using MySql.Data.MySqlClient;
using MySql.Data.Types;
using MySql.Data.Common;
using System.Data;
using System.Configuration;

namespace SkarAudioQBSync
{
    class RemoteConnection
    {
        private SshClient sshClient;
        private ForwardedPortLocal tunnel;
        public MySqlConnection connection;
        public bool createTunnel()
        {
            try
            {
                string tunnelIp = ConfigurationManager.AppSettings["tunnelIp"];
                string tunnelUser = ConfigurationManager.AppSettings["tunnelUser"];
                string privateKeyFile = ConfigurationManager.AppSettings["tunnelKeyFile"];
                sshClient = new SshClient(tunnelIp, tunnelUser, new PrivateKeyFile(@privateKeyFile));
                sshClient.Connect();

                tunnel = new ForwardedPortLocal("127.0.0.1", 3333, "127.0.0.1", 3306);
                sshClient.AddForwardedPort(tunnel);

                tunnel.Start();

                return tunnel.IsStarted;

            }
            catch (Exception ex)
            {
                Console.WriteLine("err");
                return false;
            }

           
        }
        public void connecMysql()
        {
            string mysqlServer = ConfigurationManager.AppSettings["mysqlServer"];
            string mysqlDatabase = ConfigurationManager.AppSettings["mysqlDatabase"];
            string mysqlUser = ConfigurationManager.AppSettings["mysqlUser"];
            string mysqlPassword = ConfigurationManager.AppSettings["mysqlPassword"];

            Console.WriteLine("connect to: " + mysqlDatabase);
            string connectionString = "SERVER=" + mysqlServer + ";PORT=3333;UID=" + mysqlUser + ";PASSWORD=" + mysqlPassword + ";DATABASE=" + mysqlDatabase + ";charset=utf8;";
            this.connection = new MySqlConnection(connectionString);

           
        }
        public Settings getSettings()
        {
            this.connecMysql();
            this.connection.Open();
            MySqlCommand query = new MySqlCommand("select enable_import, date_format(import_from_date, '%Y-%m-%d') as import_from_date from settings", this.connection);
            MySqlDataReader dataReader = query.ExecuteReader();
            DataTable dataTable = new DataTable();
            dataTable.BeginLoadData();
            dataTable.Load(dataReader);
            dataTable.EndLoadData();

            dataReader.Close();
            this.connection.Close();

            var settings = new Settings(dataTable.Rows[0]["enable_import"].ToString(), dataTable.Rows[0]["import_from_date"].ToString());

            return settings;

        }

        public void LogTable(string txt)
        {
            MySqlCommand query = new MySqlCommand("insert into windows_job_logs (text, created_at) VALUES (@text, NOW())", this.connection);
            query.Parameters.AddWithValue("@text", txt);
            this.connection.Open();
            query.ExecuteNonQuery();
            this.connection.Close();
        }
       
        public DataTable getImportableOrders(int orderCount, string minDate, string orderNumber = "")
        {

            connection.Open();
            try
            {
                if(orderNumber != "")
                {
                    orderNumber = "AND orderNumber='" + orderNumber + "' ";
                }

                string qrystring = "SELECT t1.isshopifyorder, " +
                    "t1.*, " +
                    "CASE WHEN products.sku IS NOT NULL THEN products.sku " +
                    "ELSE order_product.sku " +
                    "end AS sku, " +
                    "CASE " +
                    "WHEN products.sku IS NOT NULL THEN order_product.sku " +
                    "ELSE null " +
                    "end AS bundleSku,  " +
                    "CASE " +
                    "WHEN products.description IS NOT NULL THEN products.description " +
                    "ELSE order_product.name " +
                    "end AS name, " +
                    "order_product.nonInventoryItem, " +
                    "CASE " +
                    "WHEN bundle_products.quantity IS NOT NULL THEN bundle_products.quantity " +
                    "ELSE order_product.quantity " +
                    "end AS quantity, " +
                    "CASE " +
                    "WHEN bundle_products.quantity IS NOT NULL THEN 1 " +
                    "ELSE 0 " +
                    "end AS isBundle, " +
                    "order_product.unitPrice " +
                    "FROM(SELECT orders.id, " +
                    "orders.orderKey, " +
                    "orders.orderDate, " +
                    "orders.orderTotal, " +
                    "orders.orderId," +
                    "orders.orderNumber, " +
                    "orders.paymentMethod, " +
                    "orders.shippingName, " +
                    "orders.shippingCompany, " +
                    "orders.shippingStreet1, " +
                    "orders.shippingStreet2, " +
                    "orders.shippingStreet3, " +
                    "orders.shippingCity, " +
                    "orders.shippingState, " +
                    "orders.shippingPostalCode, " +
                    "orders.shippingCountry, " +
                    "orders.shippingPhone, " +
                    "orders.billingName, orders.billingCompany, orders.billingStreet1, orders.billingStreet2, " +
                    "orders.billingStreet3, orders.billingCity, orders.billingState, orders.billingPostalCode, " +
                    "orders.billingCountry, orders.billingPhone, orders.warehouseId, orders.email," +
                    "orders.isshopifyOrder, orders.shippingAmount, orders.taxAmount " +
                    "FROM orders " +
                    "WHERE orderImported = 0 " + orderNumber +
                    "AND ignoreOrder = 0 "  +
                    "AND archived = 0 " +
                    "AND error = 0 AND orderStatus = 'shipped' " +
                    "AND orders.orderDate >= @minDate " +
                    "LIMIT  0, @orderCount) t1 " +
                    "LEFT JOIN order_product " +
                    "ON order_product.orderid = t1.orderid " +
                    "LEFT JOIN bundles ON bundles.sku = order_product.sku " +
                    "LEFT JOIN bundle_products " +
                    "ON(bundle_products.bundle_id = bundles.id) " +
                    "LEFT JOIN products " +
                    "ON products.id = bundle_products.product_id;";

                MySqlCommand query = new MySqlCommand(qrystring, this.connection);

                query.Parameters.AddWithValue("@orderCount", orderCount);
                query.Parameters.AddWithValue("@minDate", minDate);
                MySqlDataReader dataReader = query.ExecuteReader();
                DataTable dataTable = new DataTable();
                dataTable.BeginLoadData();
                dataTable.Load(dataReader);
                dataTable.EndLoadData();

                dataReader.Close();
                connection.Close();

                return dataTable;

            }
            catch (Exception ex)
            {
                Console.WriteLine("MySql Connection error");
               
                return new DataTable();
            }
        }

        
        public void setImported(string orderId)
        {
            MySqlCommand query = new MySqlCommand("update orders set orderImported=1 where id=?orderId", this.connection);
            query.Parameters.AddWithValue("?orderId", orderId);
            this.connection.Open();
            query.ExecuteNonQuery();
            this.connection.Close();
        }
        public void setError(string orderId, string errorMessage)
        {
            MySqlCommand query = new MySqlCommand("update orders set error=1, errorMessage=?errorMessage where id=?orderId", this.connection);
            query.Parameters.AddWithValue("?errorMessage", errorMessage);
            query.Parameters.AddWithValue("?orderId", orderId);
            this.connection.Open();
            query.ExecuteNonQuery();
            this.connection.Close();
        }
        public void updateOrderStatus(string orderId)   
        {

            //update orders.status set saved = 1, statusText = "";
        }
        public  MySqlConnection getConnection()
        {
            return this.connection;
        }

        public void closeTunnel()
        {
            tunnel.Stop();
            sshClient.Disconnect();
            Console.WriteLine("Connection Closed");
        }
    }
}