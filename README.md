## Synopsis

The Virtual Context (VC) provides powerful querying/entity retrieval along with
"detached" change tracking. Works with LINQ to SQL (L2S) to provide a complete
dynamic data access layer. Simplifies retrieval along with creation, updating,
and destruction of complete object graphs. The VC is not a framework, and can
live side by side with other data access solutions. Designed to integrate with
both ASP.NET Web Forms and MVC.

The VC ses method chaining to provide a fully fluent interface for L2S entity
retrieval and querying. The Object Relational Mapping Utility, or ORMUtility,
exposes a rich collection of methods for manipulating object graphs. This
includes comparisons, duplication, serialization, working with or without an
entity's children and parent objects. Moreover, using the ORMUtility to
manipulate an application's entities offers the added benefit of providing the
application developer indirect access to the VC's optimized methods.

## Code Example

A few lines of code are worth a thousand words, so consider the following simple
example:

```csharp
    VirtualContext<AdentureWorksLTDataContext> vc = new VirtualContext<AdentureWorksLTDataContext>();
    Customer customer = vc.Get<Customer>().WithFullGraph().PK(29653);
    SalesOrderDetail origDetail = customer.SalesOrderHeaders.First().SalesOrderDetails.First();
    SalesOrderDetail newDetail = vc.ORMUtility.ShallowCopy(origDetail) as SalesOrderDetail;
    newDetail.SalesOrderDetailID = 0;
    newDetail.ModifiedDate = DateTime.Now;
    customer.SalesOrderHeaders.First().SalesOrderDetails.Add(newDetail);
    vc.SubmitChanges();  
```
    
In the above example, we have instantiated a `VirtualContext` object, retrieved
the complete object graph for the Customer with CustomerID 29653, and duplicated
one of its SalesOrderDetail grandchildren objects. The entire object graph that
is pulled back is change tracked by default. Meanwhile, the SalesOrderHeader
object produced by the call to `ShallowCopy()` is initially not attached to the
change tracking context. It is only when we add it to the SalesOrderDetails
collection of `customer.SalesOrderHeaders.First()`, which is change tracked,
that the VC becomes aware of newDetail. The VC recognizes that newDetail is a
new addition and when we call `vc.SubmitChanges()`, a new `SalesOrderDetail`
record is inserted into to the database.

Importantly, the VC opens two discrete connections to the data source in the
above example. The line:

```csharp
    Customer customer = vc.Get<Customer>().WithFullGraph().PK(29653);
```
    
constructs a LINQ query, instantiates a `DataContext`, retrieves the object
graph, and then disposes of the `DataContext`. Later on, the line:

```csharp
    vc.SubmitChanges();
```
    
instantiates a new `DataContext`, attaches all of the change tracked entities
with their appropriate states, invokes `DataContext.SubmitChanges()`, and then
disposes of the DataContext.

### Another Example

Let’s imagine a typical database driven web application. The end-user would like
to modify some information pertaining to a particular customer-—it really
doesn’t matter what they want to change: properties of the Customer,
add/remove/change sales information, whatever. First, we need to retrieve the
customer information from the database:

```csharp
    VirtualContext<AdentureWorksLTDataContext> vc = new VirtualContext<AdentureWorksLTDataContext>();
    Customer customer = vc.Get<Customer>()
                          .WithFullGraph()
                          .Where(c => c.CompanyName == "Some Company")
                          .Detach()
                          .Load().SingleOrDefault();
```
   
Notice the call to the `Detach()` method. This indicates that we do not want to
track this object graph. Next, we send the data off to the client by whatever
means we like. The data is altered in some way by the client in some way and the
logic within the presentation layer reconstructs the now modified object graph.
The only real requirement here is that the root object-—the Customer—retains its
original primary key. Once the modified object graph has been sent back to the
business logic layer, we could write something like:

```csharp
    public void UpdateCustomer(Customer customer)
    {
        VirtualContext<AdentureWorksLTDataContext> vc = new VirtualContext<AdentureWorksLTDataContext>();
        Customer origCustomer = vc.Get<Customer>().WithFullGraph().PK(customer.CustomerID);
        vc.ORMUtility.Modify(customer, origCustomer);
        vc.SubmitChanges();
    }
```

This method receives an untracked, modified Customer object graph. It creates
and then uses a VirtualContext to retrieve the original Customer object graph,
which is tracked by default. Next, the `Modify()` method is invoked. This method
systematically modifies the object graph referred to by origCustomer such that
it is made identical to the object graph referred to by customer. Since the
origCustomer is being change tracked, the VC is now aware of all changes that
have been made to the Customer object graph. Finally, we call
`vc.SubmitChanges()` and all changes made by the client are committed to the
database.

## Motivation

Microsoft's LINQ to SQL (L2S) is a widely used object-relational mapper,
providing access to SQL databases from within .NET's object oriented programming
environment. Unfortunately, a major shortcoming of L2S is its lack of support
for N-tier architecture. The most natural usage of L2S is to perform all data
manipulations (that is, manipulation of objects obtained from L2S queries)
within the lifespan of an L2S `DataContext` object. The `DataContext` object not
only provides access to the database via LINQ queries, but it also keeps track
of any and all changes made to the entities that such queries return. This is
convenient since all that the application logic must do, following a series of
manipulations, is call `DataContext.SubmitChanges()` and all changes are
persisted to the database. This approach, however, lacks any distinction between
business logic and data access layers.

True separation between data access and business logic requires that we dispose
of the L2S DataContext prior to manipulating our objects. This means that L2S
can no longer track our changes and we must explicitly instruct a second
DataContext, after our manipulations are complete, on how to treat each
“re-attached” entity (is it to be inserted, updated, deleted, or is it
unmodified?). This results in significant development overhead and once again
tends to blur the lines between what is business logic and what is data access
logic. The VirtualContext, a dynamic data access layer, addresses these issues
by combining “detached” change tracking with powerful object graph querying and
handling capabilities.

## Installation

This is a Visual Studio 2010 (or later) project.

## License

You are free to use, modify, and/or distribute this software under the GNU
General Public License.
