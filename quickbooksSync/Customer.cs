using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Zombie;
using Interop.QBFC13;
using System.Data;

namespace SkarAudioQBSync
{
    class Customer : QBSync
    {
        private string originalFullName;
        private string FirstName;
        private string LastName;
        private string Email;

        public string ShippingAddress1 { get; set; }
        public string ShippingAddress2 { get; set; }
        public string ShippingPostalCode { get; set; }
        public string ShippingCity { get; set; }
        public string ShippingState { get; set; }
        public string ShippingCountry { get; set; }

        public string BillingAddress1 { get; set; }
        public string BillingAddress2 { get; set; }
        public string BillingPostalCode { get; set; }
        public string BillingCity { get; set; }
        public string BillingState { get; set; }
        public string BillingCountry { get; set; }

        //We will try to load this customer by the fullname property
        public Customer(Zombie.SDKConnection cn) : base(cn)
        {

        }

        public string getOriginalFullName()
        {
            return this.originalFullName;
        }
        public void load(DataRow customer)
        {
            var batch = this.cn.NewBatch();

            var qryCust = batch.MsgSet.AppendCustomerQueryRq();

            var customerName = customer["shippingName"].ToString();
            var isShopifyOrder = customer["isShopifyOrder"].ToString();

            this.originalFullName = customerName;
            if (isShopifyOrder != "1")
            {
                customerName = "3rd Party Channel";
            }
            qryCust.ORCustomerListQuery.FullNameList.Add(customerName);


            batch.SetClosures(qryCust, b =>
            {
                var customers = new QBFCIterator<ICustomerRetList, ICustomerRet>(b);

                if (customers.Count() == 1)
                {
                   
                    Console.Write("|");
                    this.ListID = Safe.Value(customers.First().ListID);
                    this.FullName = Safe.Value(customers.First().FullName);
                    this.FirstName = Safe.Value(customers.First().FirstName);
                    this.LastName = Safe.Value(customers.First().LastName);
                    this.Email = Safe.Value(customers.First().Email);

                    this.ShippingAddress1 = customer["shippingStreet1"].ToString();
                    this.ShippingAddress2 = customer["shippingStreet2"].ToString();
                    this.ShippingCity = customer["shippingCity"].ToString();
                    this.ShippingPostalCode = customer["shippingPostalCode"].ToString();
                    this.ShippingState = customer["shippingState"].ToString();
                    this.ShippingCountry = customer["shippingCountry"].ToString();

                    if (customer["billingStreet1"].ToString() != "" && customer["billingStreet1"].ToString().Length > 2)
                    {
                        this.BillingAddress1 = customer["billingStreet1"].ToString();
                        this.BillingAddress2 = customer["billingStreet2"].ToString();
                        this.BillingCity = customer["billingCity"].ToString();
                        this.BillingPostalCode = customer["billingPostalCode"].ToString();
                        this.BillingState = customer["billingState"].ToString();
                        this.BillingCountry = customer["billingCountry"].ToString();
                    }
                    else
                    {
                        this.BillingAddress1 = customer["shippingStreet1"].ToString();
                        this.BillingAddress2 = customer["shippingStreet2"].ToString();
                        this.BillingCity = customer["shippingCity"].ToString();
                        this.BillingPostalCode = customer["shippingPostalCode"].ToString();
                        this.BillingState = customer["shippingState"].ToString();
                        this.BillingCountry = customer["shippingCountry"].ToString();
                    }

                }
                else
                {
                    Console.WriteLine("create new customer" + customer["shippingName"].ToString());
                    this.createCustomer(customer);
                }

            });

            batch.Run();
        }

        private void createCustomer(DataRow customer)
        {
            this.setFullName(customer["shippingName"].ToString());
            this.setFirstName(customer["shippingName"].ToString().Substring(0, customer["shippingName"].ToString().IndexOf(" ")));
            this.setLastName(customer["shippingName"].ToString().Substring(customer["shippingName"].ToString().IndexOf(" ") + 1));
            this.setEmail(customer["email"].ToString());

            this.ShippingAddress1 = customer["shippingStreet1"].ToString();
            this.ShippingAddress2 = customer["shippingStreet2"].ToString();
            this.ShippingCity = customer["shippingCity"].ToString();
            this.ShippingPostalCode = customer["shippingPostalCode"].ToString();
            this.ShippingState = customer["shippingState"].ToString();
            this.ShippingCountry = customer["shippingCountry"].ToString();

            if(customer["billingStreet1"].ToString() != "" && customer["billingStreet1"].ToString().Length > 2)
            {
                this.BillingAddress1 = customer["billingStreet1"].ToString();
                this.BillingAddress2 = customer["billingStreet2"].ToString();
                this.BillingCity = customer["billingCity"].ToString();
                this.BillingPostalCode = customer["billingPostalCode"].ToString();
                this.BillingState = customer["billingState"].ToString();
                this.BillingCountry = customer["billingCountry"].ToString();
            }
            else
            {
                this.BillingAddress1 = customer["shippingStreet1"].ToString();
                this.BillingAddress2 = customer["shippingStreet2"].ToString();
                this.BillingCity = customer["shippingCity"].ToString();
                this.BillingPostalCode = customer["shippingPostalCode"].ToString();
                this.BillingState = customer["shippingState"].ToString();
                this.BillingCountry = customer["shippingCountry"].ToString();
            }

            if (this.save())
            {
                Console.Write("+");
            }
        }
        public string getFirstName()
        {
            return this.FirstName;
        }
        public string getLastName()
        {
            return this.LastName;
        }
        public string getEmail()
        {
            return this.Email;
        }
        public void setFirstName(string firstName)
        {
            this.FirstName = firstName;
        }

        public void setLastName(string lastName)
        {
            this.LastName = lastName;
        }

        public void setEmail(string email)
        {
            this.Email = email;
        }
        public bool save()
        {
            Console.WriteLine("Lets add this customer");
            var insertBatch = this.cn.NewBatch();
            var qry = insertBatch.MsgSet.AppendCustomerAddRq();
            qry.Name.SetValue(this.FullName);
            qry.FirstName.SetValue(this.FirstName);
            qry.LastName.SetValue(this.LastName);

            qry.AccountNumber.SetValue("1234567");
            qry.Email.SetValue(this.Email);
            
            qry.BillAddress.Addr1.SetValue(this.BillingAddress1);
            qry.BillAddress.Addr2.SetValue(this.BillingAddress2);
            qry.BillAddress.PostalCode.SetValue(this.BillingPostalCode);
            qry.BillAddress.City.SetValue(this.BillingCity);
            qry.BillAddress.Country.SetValue(this.BillingCountry);

            qry.ShipAddress.Addr1.SetValue(this.ShippingAddress1);
            qry.ShipAddress.Addr2.SetValue(this.ShippingAddress2);
            qry.ShipAddress.PostalCode.SetValue(this.ShippingPostalCode);
            qry.ShipAddress.City.SetValue(this.ShippingCity);
            qry.ShipAddress.Country.SetValue(this.ShippingCountry);


            insertBatch.SetClosures(qry, b =>
            {
                var customer =  b as ICustomerRet;
                this.ListID = Safe.Value(customer.ListID);
                Console.WriteLine("Im awesome if this works");
                Console.WriteLine(Safe.Value(customer.Email));
            });
                insertBatch.Run();

            return true;
        }
    }
}
