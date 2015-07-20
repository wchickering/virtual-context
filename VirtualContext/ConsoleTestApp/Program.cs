using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq;
using System.Reflection;
using System.Transactions;
using AdventureWorksLT;
using Schrodinger.L2SHelper;
using Schrodinger.EntityTracker;
using Schrodinger.VirtualContext;
using Schrodinger.EntityFactory;

namespace ConsoleTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("BEGINNING TESTS. . . ");
            Console.WriteLine();

            /*** Begin Tests ***/

            VirtualContext<AdventureWorksLTDataContext> vc = new VirtualContext<AdventureWorksLTDataContext>();

            ////EntitySet<Customer> customers = vc.Get<Customer>().WithFullGraph().Load();

            //int count = 0;

            //Console.WriteLine("Counting via DataContext directly.");
            //using (AdventureWorksLTDataContext context = new AdventureWorksLTDataContext())
            //{
            //    context.Log = Console.Out;
            //    count = (from c in context.GetTable<Customer>()
            //                 select c).Count();
            //    Console.WriteLine("There are " + count + " Customers in the DB.");
            //}
            //Console.WriteLine();

            //Console.WriteLine("Counting via VirtualContext.");
            //vc.Log = Console.Out;
            //count = vc.Get<Customer>().Count();
            //Console.WriteLine("There are " + count + " Customers in the DB.");
            //Console.WriteLine();

            //Console.WriteLine("Testing vc counting functionality vis a vis query batching.");
            //var countQuery = vc.GetCount(typeof(Customer));
            //var customerQuery = vc.Get(typeof(Customer));
            //vc.LoadPending();
            //count = countQuery.GetCount();
            //EntitySet<Customer> customers = customerQuery.GetData() as EntitySet<Customer>;
            //Console.WriteLine("There are " + count + " Customers in the DB.");
            //Console.WriteLine("Customer EntitySet contains " + customers.Count() + " entities.");
            //Console.WriteLine();


            Customer customer = vc.Get<Customer>().WithFullGraph().PK(29653);

            Console.WriteLine("--------- EntityStates: ---------");
            foreach (EntityState state in vc.EntityStates)
            {
                Console.WriteLine(state.Entity.GetType() + " has status " + state.Status);
            }
            Console.WriteLine();

            Console.WriteLine("Duplicating object graph.");
            Customer dupCust = vc.ORMUtility.Duplicate(customer) as Customer;

            Console.WriteLine("--------- EntityStates: ---------");
            foreach (EntityState state in vc.EntityStates)
            {
                Console.WriteLine(state.Entity.GetType() + " has status " + state.Status);
            }
            Console.WriteLine();

            //Console.WriteLine("Modifying a SaleOrderDetail of the duplicated graph.");
            //SalesOrderDetail detail = dupCust.SalesOrderHeaders.First().SalesOrderDetails.First();
            //detail.UnitPrice += 0.01M;
            ////dupCust.SalesOrderHeaders.First().SalesOrderDetails.Remove(detail);
            ////SalesOrderDetail newDetail = vc.ORMUtility.ShallowCopy(detail) as SalesOrderDetail;
            ////newDetail.SalesOrderDetailID = 0;
            ////dupCust.SalesOrderHeaders.First().SalesOrderDetails.Add(newDetail);

            //Console.WriteLine("--------- EntityStates: ---------");
            //foreach (EntityState state in vc.EntityStates)
            //{
            //    Console.WriteLine(state.Entity.GetType() + " has status " + state.Status);
            //}
            //Console.WriteLine();   

            //Console.WriteLine("Calling vc.ORMUtility.Modify(dupCust, customer)");
            //vc.ORMUtility.Modify(dupCust, customer);

            //Console.WriteLine("--------- EntityStates: ---------");
            //foreach (EntityState state in vc.EntityStates)
            //{
            //    Console.WriteLine(state.Entity.GetType() + " has status " + state.Status);
            //}
            //Console.WriteLine();

            

            //if (object.ReferenceEquals(customer, dupCust))
            //{
            //    Console.WriteLine("Root references are equal!!!");
            //}
            //else
            //{
            //    Console.WriteLine("Root references are NOT equal.");
            //}
            //if (object.Equals(customer, dupCust))
            //{
            //    Console.WriteLine("Root objects are equal by value.");
            //}
            //else
            //{
            //    Console.WriteLine("Root object NOT equal by value!!!");
            //}
            //if (vc.ORMUtility.ShallowCompare(customer, dupCust))
            //{
            //    Console.WriteLine("Root objects equal as per ShallowCompare().");
            //}
            //else
            //{
            //    Console.WriteLine("Root object NOT equal as per ShallowCompare()!!!");
            //}
            //if (vc.ORMUtility.DeepCompare(customer, dupCust))
            //{
            //    Console.WriteLine("Root objects equal as per DeepCompare().");
            //}
            //else
            //{
            //    Console.WriteLine("Root object NOT equal as per DeepCompare()!!!");
            //}

            
            



            //SalesOrderDetail origDetail = customer.SalesOrderHeaders.First().SalesOrderDetails.First();
            //SalesOrderDetail newDetail = vc.ORMUtility.ShallowCopy(origDetail) as SalesOrderDetail;
            //newDetail.SalesOrderDetailID = 0;
            //newDetail.ModifiedDate = DateTime.Now;
            //customer.SalesOrderHeaders.First().SalesOrderDetails.Add(newDetail);
            //vc.SubmitChanges();

            //foreach (SalesOrderDetail detail in customer.SalesOrderHeaders.First().SalesOrderDetails)
            //{
            //    foreach (PropertyInfo property in vc.ORMUtility.GetDBColProperties(typeof(SalesOrderDetail)))
            //    {
            //        if (property.Name != "rowguid")
            //        {
            //            Console.Write(property.Name + " = " + property.GetValue(detail, null).ToString() + "|");
            //        }
            //    }
            //    Console.WriteLine();
            //}

            //VirtualContext<AdventureWorksLTDataContext> vc = new VirtualContext<AdventureWorksLTDataContext>();
            //Product newProduct = new Product()
            //{
            //    Name = "LL Touring Frame - Black, 62",
            //    ProductNumber = "FR-T55B-62",
            //    Color = "Black",
            //    StandardCost = 265.20M,
            //    ListPrice = 390.10M,
            //    Size = "62",
            //    Weight = 1451.49M,
            //    SellStartDate = DateTime.Now,
            //    SellEndDate = null,
            //    DiscontinuedDate = null,
            //    ModifiedDate = DateTime.Now
            //};
            //ProductModel productModel = vc.Get<ProductModel>().Where(m => m.Name == "LL Touring Frame").Load().SingleOrDefault();
            //ProductCategory productCategory = vc.Get<ProductCategory>().Where(c => c.Name == "Touring Bikes").Load().SingleOrDefault();
            //newProduct.ProductModel = productModel;
            //newProduct.ProductCategory = productCategory;
            //vc.SubmitChanges();







            //Console.WriteLine("---------- DependentTree ---------");
            //foreach (object entity in vc.ORMUtility.ToDependentTree(customer))
            //{
            //    Console.WriteLine(entity.GetType().Name + " has status " + vc.GetEntityStatus(entity));
            //}
            //Console.WriteLine();

            //Console.WriteLine("---------- FlatGraph ---------");
            //foreach (object entity in vc.ORMUtility.ToFlatGraph(customer))
            //{
            //    Console.WriteLine(entity.GetType().Name + " has status " + vc.GetEntityStatus(entity));
            //}
            //Console.WriteLine();

            //Product firstProduct = customer.SalesOrderHeaders.First().SalesOrderDetails.First().Product;
            //Console.WriteLine("firstProduct ID=" + firstProduct.ProductID + ": " + firstProduct.Name);
            //Console.WriteLine();
            //Console.WriteLine("Attempting to modify product name.");
            //firstProduct.Name += "something";


            
            
            
            
            //Console.WriteLine("Before serialization, ID=" + customer.CustomerID + ": " + customer.CompanyName + " has " + customer.SalesOrderHeaders.First().SalesOrderDetails.Count() + " details.");
            //string serializedEntity = ormUtility.SerializeEntity(customer);
            //Customer deserializedCustomer = ormUtility.DeserializeEntity(typeof(Customer), serializedEntity) as Customer;
            //Console.WriteLine("After serialization, ID=" + customer.CustomerID + ": " + customer.CompanyName + " has " + deserializedCustomer.SalesOrderHeaders.First().SalesOrderDetails.Count() + " details.");

            //List<EntityState> EntityStates = vc.EntityStates.ToList();
            //EntityState state = EntityStates.First();
            //Console.WriteLine("The first EntityState before serialization: " + state.Entity.GetType().Name + " marked for " + state.Status);
            //Console.WriteLine("vc.IsTracking(" + state.Entity.GetType() + ") = " + vc.IsTracking(state.Entity));
            //string serializedEntityState = ormUtility.SerializeEntity(state);
            //EntityState deserializedState = ormUtility.DeserializeEntity(typeof(EntityState), serializedEntityState) as EntityState;
            //Console.WriteLine("The first EntityState after deserialization: " + state.Entity.GetType().Name + " marked for " + state.Status);
            //Console.WriteLine("(after deserialization) vc.IsTracking(" + deserializedState.Entity.GetType() + ") = " + vc.IsTracking(deserializedState.Entity));

            //string allEntityStatesSerialized = ormUtility.SerializeEntity(EntityStates);
            //List<EntityState> deserializedEntityStates = ormUtility.DeserializeEntity(EntityStates.GetType(), allEntityStatesSerialized) as List<EntityState>;

            //Customer dsCust = deserializedEntityStates.First().Entity as Customer;
            //SalesOrderHeader dsHeader = null;
            //foreach (EntityState entityState in deserializedEntityStates)
            //{
            //    dsHeader = entityState.Entity as SalesOrderHeader;
            //    if (dsHeader != null)
            //    {
            //        break;
            //    }
            //}
            //if (object.ReferenceEquals(dsCust, dsHeader.Customer))
            //{
            //    Console.WriteLine("References preserved!");
            //}
            //else
            //{
            //    Console.WriteLine("Duplicate entities created.");
            //}


            //int pageNum = 1;
            ////var products = vc.Get<Product>().OrderByDescending(p => p.Name).PageSize(10).PageNum(pageNum++).Load();
            //var products = vc.Get<Product>().OrderBy(p => p.Name).Load();
            ////while (products.Count() > 0)
            ////{
            //    foreach (Product prod in products)
            //    {
            //        foreach (PropertyInfo property in ormUtility.GetDBColProperties(typeof(Product)))
            //        {
            //            if (property.Name != "ThumbNailPhoto" &&
            //                property.Name != "rowguid")
            //            {
            //                object val = property.GetValue(prod, null);
            //                Console.Write(property.Name + " = ");
            //                if (val != null)
            //                {
            //                    Console.Write(val.ToString() + "|");
            //                }
            //                else
            //                {
            //                    Console.Write("NULL|");
            //                }
            //            }
            //        }
            //        Console.WriteLine();
            //        //Console.WriteLine(prod.Name);
            //    }
            //    Console.WriteLine();

            //    products = vc.Get<Product>().OrderByDescending(p => p.Name).PageSize(10).PageNum(pageNum++).Load();
            //}



            //EntitySet<Customer> customersWithSales = vc.Get<Customer>().WithFullGraph().Detach().Where(c => c.SalesOrderHeaders.Count() > 0).OrderByDescending(c => c.SalesOrderHeaders.Single().SalesOrderID).Load();
            //Console.WriteLine("First 10 Customers with at least one sale:");
            //int i = 0;
            //foreach (Customer cust in customersWithSales)
            //{
            //    int detailsCnt = (from d in ormUtility.ToEntityTree(cust)
            //                      where d.GetType() == typeof(SalesOrderDetail)
            //                      select d).Count();
            //    Console.WriteLine("ID=" + cust.CustomerID + ": " + cust.CompanyName + " has " + cust.SalesOrderHeaders.Count() + " headers and " + detailsCnt + " details.");
                
            //    //foreach (SalesOrderHeader head in cust.SalesOrderHeaders)
            //    //{
            //    //    foreach (SalesOrderDetail det in head.SalesOrderDetails)
            //    //    {
            //    //        Console.WriteLine("ID=" + det.SalesOrderDetailID);
            //    //    }
            //    //}
            //    //Console.WriteLine();
                
            //    if (i++ >= 10) break;
            //}

            //Console.WriteLine("Constructing EntityQueries.");
            ////var custQuery = vc.Get<Customer>().WithDependents().Where(c => c.SalesOrderHeaders.Count() > 0);
            //var custQuery = vc.Get<Customer>()
            //    .LoadWith<Customer>(c => c.SalesOrderHeaders.First().SalesOrderDetails)
            //    .LoadWith<SalesOrderHeader>(s => s.SalesOrderDetails)
            //    .Where(c => c.CustomerID == 29653);
            ////var custQuery = vc.Get(typeof(Customer)).WithDependents();
            //var prodQuery = vc.Get<Product>().WithFKs();
            //var addrQuery = vc.Get<CustomerAddress>();
            //Console.WriteLine("Retrieving both queries via one DataConnection.");
            //vc.LoadPending();
            //Console.WriteLine("Obtaining data from EntityQueries.");
            ////EntitySet<Customer> customers = custQuery.GetData() as EntitySet<Customer>;
            //Customer customer = custQuery.GetData().SingleOrDefault();
            //EntitySet<Product> products = prodQuery.GetData();
            //EntitySet<CustomerAddress> addresses = addrQuery.GetData();
            //Console.WriteLine("Obtain customer ID=" + customer.CustomerID + ": " + customer.CompanyName + " with " + customer.SalesOrderHeaders.Count() + " sales headers.");
            //Console.WriteLine("Obtained " + products.Count() + " products and " + addresses.Count() + " CustomerAddresses.");




            ////Customer thrilling = (from c in customersWithSales
            ////                     where c.CustomerID == 29653
            ////                     select c).SingleOrDefault();
            //Customer thrilling = vc.Get<Customer>().WithFullGraph().PK(29653);

            ////Console.WriteLine("Before edit, CompanyName = " + thrilling.CompanyName + ".");
            //Console.WriteLine("EntityStates:");
            //foreach (EntityState entityState in vc.EntityStates)
            //{
            //    Console.WriteLine(entityState.Entity.GetType().Name + " " + entityState.Status + (entityState.IsRoot ? " is Root." : "."));
            //}
            ////thrilling.CompanyName = "The Thrilling Bike Store";
            ////Console.WriteLine("After edit, CompanyName = " + thrilling.CompanyName + ".");
            ////Console.WriteLine("EntityStates:");
            ////foreach (EntityState entityState in vc.EntityStates)
            ////{
            ////    Console.WriteLine(entityState.Entity.GetType().Name + " " + entityState.Status);
            ////}
            ////Console.WriteLine("VirtualContext submitting changes. . .");
            ////vc.SubmitChanges();
            ////Console.WriteLine("Querying customer back. . .");
            ////Customer editedThrilling = vc.Get<Customer>().PK(29653);
            ////Console.WriteLine("Back from data source, CompanyName = " + editedThrilling.CompanyName + ".");

            ////foreach (SalesOrderDetail detail in thrilling.SalesOrderHeaders.Single().SalesOrderDetails.Where(d => d.ProductID == 789).ToList())
            ////{
            ////    thrilling.SalesOrderHeaders.Single().SalesOrderDetails.Remove(detail);
            ////}

            //foreach (SalesOrderDetail detail in thrilling.SalesOrderHeaders.Single().SalesOrderDetails)
            //{
            //    foreach (PropertyInfo column in ormUtility.GetDBColProperties(typeof(SalesOrderDetail)))
            //    {
            //        Console.Write(column.Name + " = " + column.GetValue(detail, null) + "|");
            //    }
            //    Console.WriteLine();
            //}

            //Product product = vc.Get(typeof(Product)).WithFKs().PK(789) as Product;
            //Console.WriteLine("ProductID=" + product.ProductID + " has ModelID=" + product.ProductModelID + ": " + product.ProductModel.Name);

            //for (int j = 0; j < 5; j++)
            //{
            //    SalesOrderDetail sale = new SalesOrderDetail();
            //    sale.ModifiedDate = DateTime.Now;
            //    sale.Product = product;
            //    sale.OrderQty = 1;
            //    sale.UnitPrice = 1.0M;
            //    sale.UnitPriceDiscount = 1.0M;
            //    thrilling.SalesOrderHeaders.First().SalesOrderDetails.Add(sale);
            //}
            //Console.WriteLine("EntityStates:");
            //foreach (EntityState entityState in vc.EntityStates)
            //{
            //    Console.WriteLine(entityState.Entity.GetType().Name + " " + entityState.Status + (entityState.IsRoot ? " is Root." : "."));
            //}

            //Console.WriteLine();
            //Console.WriteLine("Submitting changes. . .");
            //vc.SubmitChanges();
            //Console.WriteLine();

            //Console.WriteLine("EntityStates:");
            //foreach (EntityState entityState in vc.EntityStates)
            //{
            //    Console.WriteLine(entityState.Entity.GetType().Name + " " + entityState.Status + (entityState.IsRoot ? " is Root." : "."));
            //}



            




            //Console.WriteLine("Reading all customers, full graph, with > 0 orders via VirtualContext with tracking on.");
            //EntitySet<Customer> customers = vc.Get<Customer>().WithFullGraph().Where(c => c.SalesOrderHeaders.Count() > 0).Load();
            //Console.WriteLine("Currently tracking: " + vc.EntityStates.Count() + ".");

            //Console.WriteLine("Reading all customers, full graph, with > 0 orders via VirtualContext WITHOUT tracking.");
            //customers = vc.Get<Customer>().WithFullGraph().Where(c => c.SalesOrderHeaders.Count() > 0).Detach().Load();
            //Console.WriteLine("Currently tracking: " + vc.EntityStates.Count() + ".");
            
            
            
            //Console.WriteLine(customers.Count() + " Customers with at least one order.");
            //Console.WriteLine("The first 10 are:");
            //int i = 0;
            //foreach (Customer cust in customers)
            //{
            //    Console.WriteLine("ID=" + cust.CustomerID + ": " + cust.CompanyName + ".");
            //    if (++i >= 10) break;
            //}

            //Customer customer1 = customers.First();





            //Console.WriteLine("Generating EntityTree on first Customer: " + customer1.CompanyName + ".");
            //EntityTree<AdventureWorksLTDataContext> entityTree = new EntityTree<AdventureWorksLTDataContext>(customer1);
            ////foreach (object item in entityTree)
            ////{
            ////    Console.WriteLine(item.GetType());
            ////}
            //List<Type> entityTypes = new List<Type>();
            //foreach (object item in entityTree)
            //{
            //    if (!entityTypes.Contains(item.GetType()))
            //    {
            //        entityTypes.Add(item.GetType());
            //    }
            //}
            //foreach (Type type in entityTypes)
            //{
            //    int count = (from e in entityTree
            //                 where e.GetType() == type
            //                 select e).Count();

            //    Console.WriteLine("There are " + count + " " + type + ".");
            //}

            //Console.WriteLine();
            //Console.WriteLine("A list of Entity Types:");
            //foreach (Type type in ormUtility.GetAllEntityTypes())
            //{
            //    Console.WriteLine(type);
            //}




            //EntityTracker<AdventureWorksLTDataContext> tracker = new EntityTracker<AdventureWorksLTDataContext>();
            //tracker.Track(customer1);
            //tracker.SetOriginal(customer1);

            //CustomerAddress customerAddress = customer1.CustomerAddresses.First();
            //Console.WriteLine("CustomerAddress.AddressID=" + customerAddress.AddressID + ".");
            //Address address = customerAddress.Address;
            //if (address != null)
            //{
            //    Console.WriteLine("Address ID=" + address.AddressID + ": " + address.AddressLine1 + ".");
            //}
            //else
            //{
            //    Console.WriteLine("CustomerAddress.Address == null.");
            //}

            
            //tracker.SetInsert(customer1);
            //Console.WriteLine("Modifying Customer object.");
            //customer1.FirstName = "Bobby";
            //customer1.SalesOrderHeaders.Clear();
            //SalesOrderHeader header = customer1.SalesOrderHeaders.First();
            //header.SalesOrderDetails.Clear();
            //header.Customer = null;

            //SalesOrderHeader header1 = new SalesOrderHeader();
            //customer1.SalesOrderHeaders.Add(header1);
            //WriteEntityTree(tracker);

            //SalesOrderHeader header2 = new SalesOrderHeader();
            //customer1.SalesOrderHeaders.Add(header2);
            //SalesOrderDetail detail1 = new SalesOrderDetail();
            //SalesOrderDetail detail2 = new SalesOrderDetail();
            //header2.SalesOrderDetails.Add(detail1);
            //header2.SalesOrderDetails.Add(detail2);
            //Address address1 = vc.Get<Address>().Load().First();
            //tracker.Track(address1);
            //tracker.SetOriginal(address1);
            //header2.Address = address1;
            
            

            //using (AdventureWorksLTDataContext context = new AdventureWorksLTDataContext())
            //{
            //    using (TransactionScope scope = new TransactionScope())
            //    {
            //        context.DeferredLoadingEnabled = false;
            //        DataContextSyncTool<AdventureWorksLTDataContext> syncTool =
            //            new DataContextSyncTool<AdventureWorksLTDataContext>(context, tracker);
            //        WriteEntityTree(tracker);
            //        Console.WriteLine("Syncing to DataContext. . .");
            //        syncTool.Sync();
            //        Console.WriteLine("Submitting changes to DB. . .");
            //        context.SubmitChanges();
            //        Console.WriteLine("Resetting Tracker. . .");
            //        tracker.ResetTracking();
            //        WriteEntityTree(tracker);
            //        Console.WriteLine("Rolling back transaction. . .");
            //    }
            //}

            //WriteEntityTree(tracker);
 


            /*** End Tests ***/

            Console.WriteLine();
            Console.WriteLine("Press Enter to Exit.");
            Console.ReadLine();
        }

        static void WriteEntityTree(EntityTracker<AdventureWorksLTDataContext> tracker)
        {
            Console.WriteLine("---------------------------------------");
            Console.WriteLine("Entity Tree:");
            foreach (EntityState entityState in tracker.EntityStates)
            {
                Console.WriteLine(entityState.Entity.GetType().Name + ": " + entityState.Status + (entityState.IsRoot?" is Root.":"."));
            }
            Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
        }
    }
}
