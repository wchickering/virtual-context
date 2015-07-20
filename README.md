## Synopsis

The Virtual Context (VC) provides powerful querying/entity retrieval along with
"detached" change tracking. Works with LINQ to SQL (L2S) to provide a complete
dynamic data access layer. Simplifies retrieval along with creation, updating,
and destruction of complete object graphs. The VC is not a framework, and can
live side by side with other data access solutions. Designed to integrate with
both ASP.NET Web Forms and MVC.

The VC uses method chaining to provide a fully fluent interface for L2S entity
retrieval and querying. The Object Relational Mapping Utility, or ORMUtility,
exposes a rich collection of methods for manipulating object graphs. This
includes comparisons, duplication, serialization, working with or without an
entity's children and parent objects. Moreover, using the ORMUtility to
manipulate an application's entities offers the added benefit of providing the
application developer indirect access to the VC's optimized methods.

## Code Example

Consider the following simple example, which uses the AdventureWorksLT Sample
database:

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
collection of `customer.SalesOrderHeaders.First()`, which *is* change tracked,
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

## Architecture and API

The VC has a highly modular design, consisting of the following discrete
components:

  * `EntityFactory`
  * `EntityTracker`
  * `ORMUtility`
  * `L2SDataSyncTool`
  * `L2SDataConnection`

The VC implements the `IEntityFactory` and `IEntityTracker` interfaces, acting
as a wrapper for the two corresponding classes. Both the `EntityFactory` and
`EntityTracker` are fully abstracted/decoupled from L2S. Rather, they interface
with L2S using a dependency injection pattern via the `L2SDataSyncTool`, the
`L2SDataConnection`, and the O/R multipurpose tool, the `ORMUtility`.  The name
"EntityFactory" is perhaps a misnomer; this class actually produces an object
called an `EntityQuery`, which contains all the information required to
construct a LINQ query that will retrieve the desired object, object graph,
object set, or set of object graphs. Calling an `EntityQuery`’s `Load()` method
causes the `EntityQuery` to request that the `EntityFactory` fill its
`DataRequest`.  The `EntityFactory` then instantiates an `L2SDataConnection`,
which in turn instantiates the `DataContext` that is ultimately used to retrieve
the requested object(s).

The `EntityQuery` class provides the application developer with a rich API in
the form of a method chaining pattern. This API includes the following methods
that may be used to tailor a specific object request:

  * `PK(object pk)`
  * `MatchPK(object entity)`
  * `Where(Expression<Func<TEntity, bool>> predicate)`
  * `OrderBy<TKey>(Expression<Func<TEntity, TKey>> expression)`
  * `OrderByDescending<TKey>(Expression<Func<TEntity, TKey>> expression)`
  * `PageSize(int pageSize)`
  * `PageNum(int pageNum)`
  * `LoadWith<T>(Expression<Func<T, object>> expression)`
  * `WithFKs()`
  * `WithDependents()`
  * `WithFullGraph()`
  * `WithFullGraphNoFKs()`
  * `Detach()`
 
What’s more, there are in fact two `EntityQuery` classes: one that utilizes
*generic* methods for strongly typed querying and another, more dynamic version,
utilizing *reflection*, for when object types are not known at compile time. The
following code illustrates the difference between these two:

```csharp
Product product = vc.Get<Product>()
                    .Where(p => p.Name == "Some Product")
                    .LoadWith(p => p.ProductModel)
                    .LoadWith(p => p.ProductCategory)
                    .Load().SingleOrDefault();
object entity = vc.Get(someDynamicType)
                  .Where(someDynamicLambdaExpression)
                  .WithFKs()
                  .Load().SingleOrDefault();
```

Like the `EntityFactory`, the `EntityTracker` is fully decoupled from L2S; all of
its interactions with L2S occur via the `ORMUtility`. The `EntityTracker` internally
maintains a tracking context by binding event handlers to the `PropertyChanging`
and `PropertyChanged` events of the tracked entities.
A great deal of functionality resides in the `ORMUtility`, which is utilized by
all of the aforementioned classes. To minimize overhead, the same instance of
the `ORMUtility` that the VC exposes to the application developer is
shared by the `EntityTracker`, the `L2SDataSyncTool`, the `EntityFactory`, all
`EntityQuery` objects, etc. An important feature of this helper class is that it
caches object model information within nested Generic Dictionary structures
(such structures have been shown to have shorter lookup times than comparable
Hashtable structures). This results in faster object graph and object model
traversals, and hence, overall improved performance.

A subset of the `IORMUtility` interface follows:

  * `IEnumerable<Type> GetAllEntityTypes()`
  * `IEnumerable <PropertyInfo> GetForeignKeyRefs(Type type)`
  * `IEnumerable <PropertyInfo> GetDependents(Type type)`
  * `IEnumerable <PropertyInfo> GetDBColProperties(Type type)`
  * `IEnumerable <PropertyInfo> GetPrimaryKeys(Type type)`
  * `IEnumerable <PropertyInfo> GetDBGenProperties(Type type)`
  * `bool HasPK(object entity)`
  * `bool PKCompare(object entityA, object entityB)`
  * `bool ShallowCompare(object entityA, object entityB)`
  * `bool DeepCompare(object reference, object target)`
  * `object ShallowCopy(object entity)`
  * `object Duplicate(object entity)`
  * `void Modify(object reference, object target)`
  * `void PKCopy(object fromEntity, object toEntity)`
  * `PropertyInfo GetFKOtherKey(PropertyInfo foreignKeyRef)`
  * `PropertyInfo GetFKAssociation(PropertyInfo thisKeyProperty)`
  * `bool IsForeignKey(PropertyInfo property)`
  * `bool IsDependent(PropertyInfo property)`
  * `bool IsDBColumnProperty(PropertyInfo property)`
  * `bool IsPrimaryKey(PropertyInfo property)`
  * `bool IsDbGenerated(PropertyInfo property)`
  * `string SerializeEntity(object entity)`
  * `object DeserializeEntity(Type type, string serializedEntity)`
  * `IEnumerable<object> ToDependentTree(object entity)`
  * `IEnumerable<object> ToFKTree(object entity)`
  * `IEnumerable<object> ToFlatGraph(object entity)`

## More Code Examples

*As with the above examples, these use the AdventureWorksLT Sample database.*

Inserting a new `Product`:

```csharp
VirtualContext<AdentureWorksLTDataContext> vc = new VirtualContext<AdentureWorksLTDataContext>();
Product newProduct = new Product()
{
    Name = "LL Touring Frame - Black, 62",
    ProductNumber = "FR-T55B-62",
    Color = "Black",
    StandardCost = 265.20M,
    ListPrice = 390.10M,
    Size = "62",
    Weight = 1451.49M,
    SellStartDate = DateTime.Now,
    SellEndDate = null,
    DiscontinuedDate = null,
    ModifiedDate = DateTime.Now,
    ProductModelID = 10,
    ProductCategoryID = 7
};
vc.Track(newProduct);
vc.SubmitChanges();
```
    
Alternative approach to inserting the same new `Product`:

```csharp
VirtualContext<AdentureWorksLTDataContext> vc = new VirtualContext<AdentureWorksLTDataContext>();
Product newProduct = new Product()
{
    Name = "LL Touring Frame - Black, 62",
    ProductNumber = "FR-T55B-62",
    Color = "Black",
    StandardCost = 265.20M,
    ListPrice = 390.10M,
    Size = "62",
    Weight = 1451.49M,
    SellStartDate = DateTime.Now,
    SellEndDate = null,
    DiscontinuedDate = null,
    ModifiedDate = DateTime.Now
};
newProduct.ProductModel = vc.Get<ProductModels>()
                            .Where(m => m.Name == "LL Touring Frame")
                            .Load().SingleOrDefault();
newProduct.ProductCategory = vc.Get<ProductCategory>()
                               .Where(c => c.Name == "Touring Bikes")
                               .Load().SingleOrDefault();
vc.SubmitChanges();
```
    
In the last example, the `vc.Track()` method is not called. This is because our
new product is automatically attached to the tracking context as a consequence
of the line:

```csharp
newProduct.ProductModel = vc.Get<ProductModel>()
                            .Where(m => m.Name == "LL Touring Frame")
                            .Load().SingleOrDefault();
```
	
in which we add the initially untracked `Product` to the `Product` collection of
the tracked `ProductModel` that is returned by the VC.

Also note that in the last example, there are (implicitly) three instantiations
of the `DataContext` class, one for each of the following lines:

```csharp
newProduct.ProductModel = vc.Get<ProductModel>()
                            .Where(m => m.Name == "LL Touring Frame")
                            .Load().SingleOrDefault();
newProduct.ProductCategory = vc.Get<ProductCategory>()
                               .Where(c => c.Name == "Touring Bikes")
                               .Load().SingleOrDefault();
vc.SubmitChanges();
```
	
We can reduce this to only two instantiations by exploiting the batch query
feature of the VirtualContext. Those three lines of code can be replaced with:

```csharp
var productModelQuery = vc.Get<ProductModel>()
                          .Where(m => m.Name == "LL Touring Frame");
var productCategoryQuery = vc.Get<ProductCategory>()
                             .Where(c => c.Name == "Touring Bikes");
vc.LoadPending();
newProduct.ProductModel = productModelQuery.GetData().SingleOrDefault();
newProduct.ProductCategory = productCategoryQuery.GetData().SingleOrDefault();
vc.SubmitChanges();
```
	
The call to `vc.LoadPending()` executes both queries within a single
`DataContext`. Sets of records can also be pulled back. For example:

```csharp
EntitySet<Product> products = vc.Get<Product>()
                                .WithFKs().Detach().Load();
```
	
Here we have pulled back all of the products, along with all of their foreign
key objects. We have included a call to Detach(), which instructs the VC to not
track these objects, in order to avoid unnecessary overhead. If, however, we
were pulling back these objects for display on a collections page that provides
pagination, we certainly would not want all of the products. We can include
pagination and sorting instructions as follows:

```csharp
EntitySet<Product> products = vc.Get<Product>()
                                .WithFKs().Detach()
                                .OrderBy(p => p.ProductCategory.Name)
                                .PageSize(30).PageNum(2).Load();
```
	
This time, we only retrieved 30 objects. We are also retrieving the products
ordered by their `ProductCategory.Name` and we have skipped the first 30
objects; we receive objects 31 through 60.

## Installation

This is a Visual Studio 2010 (or later) project.

## License

You are free to use, modify, and/or distribute this software under the GNU
General Public License.
