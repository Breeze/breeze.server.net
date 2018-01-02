using Foo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace Models.NorthwindIB.CF {


  public class NorthwindIBContext_CF : DbContext {
    
    public DbContextOptions<NorthwindIBContext_CF> Options {
      get; private set;
    }

    public NorthwindIBContext_CF(DbContextOptions<NorthwindIBContext_CF> options)
      : base(options) {
      // Hang onto it so that we can create a clone.
      Options = options;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
      modelBuilder.Entity<Comment>()
         .HasKey(c => new { c.CreatedOn, c.SeqNum });
      modelBuilder.Entity<OrderDetail>()
        .HasKey(od => new { od.OrderID, od.ProductID });
      modelBuilder.Entity<OrderDetail>().Property(od => od.OrderID).ValueGeneratedNever();
      modelBuilder.Entity<Supplier>()
        .OwnsOne<Location>(s => s.Location);

    }


    #region DbSets

    public DbSet<Category> Categories { get; set; }

    public DbSet<Customer> Customers { get; set; }

    public DbSet<Employee> Employees { get; set; }

    public DbSet<EmployeeTerritory> EmployeeTerritories { get; set; }

    public DbSet<Order> Orders { get; set; }

    public DbSet<OrderDetail> OrderDetails { get; set; }

    public DbSet<PreviousEmployee> PreviousEmployees { get; set; }

    public DbSet<Product> Products { get; set; }

    public DbSet<Region> Regions { get; set; }

    public DbSet<Role> Roles { get; set; }

    public DbSet<Supplier> Suppliers { get; set; }

    public DbSet<Territory> Territories { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<UserRole> UserRoles { get; set; }

    public DbSet<InternationalOrder> InternationalOrders { get; set; }

    public DbSet<TimeLimit> TimeLimits { get; set; }

    public DbSet<TimeGroup> TimeGroups { get; set; }

    public DbSet<Comment> Comments { get; set; }

    // public DbSet<Geospatial> Geospatials { get; set; }

    public DbSet<UnusualDate> UnusualDates { get; set; }

    #endregion EntityQueries
  }
}

namespace Foo {

  [AttributeUsage(AttributeTargets.Class)] // NEW
  public class CustomerValidator : ValidationAttribute {
    public override Boolean IsValid(Object value) {
      var cust = value as Customer;
      if (cust != null && cust.CompanyName.ToLower() == "error") {
        ErrorMessage = "This customer is not valid!";
        return false;
      }
      return true;
    }
  }

  [AttributeUsage(AttributeTargets.Property)]
  public class CustomValidator : ValidationAttribute {
    public override Boolean IsValid(Object value) {
      try {
        string val = (string)value;
        if (!string.IsNullOrEmpty(val) && val.StartsWith("Error")) {
          ErrorMessage = "{0} equals the word 'Error'";
          return false;
        }
        return true;
      } catch (Exception e) {
        var x = e;
        return false;
      }
    }
  }

  #region Category class

  [Table("Category", Schema = "dbo")]
  public partial class Category {

    #region Data Properties

    /// <summary>Gets or sets the CategoryID. </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("CategoryID")]
    public int CategoryID { get; set; }

    /// <summary>Gets or sets the CategoryName. </summary>
    [Column("CategoryName")]
    public string CategoryName { get; set; }

    /// <summary>Gets or sets the Description. </summary>
    [Column("Description")]
    public string Description { get; set; }

    /// <summary>Gets or sets the Picture. </summary>
    [Column("Picture")]
    public byte[] Picture { get; set; }

    /// <summary>Gets or sets the RowVersion. </summary>
    [Column("RowVersion")]
    [DefaultValue(2)]
    public int RowVersion { get; set; }

    #endregion Data Properties

    #region Navigation properties

    /// <summary>Gets the Products. </summary>
    // [DataMember]
    [InverseProperty("Category")]
    public ICollection<Product> Products { get; set; }

    #endregion Navigation properties


  }

  #endregion Category class

  #region Customer class

  [Table("Customer", Schema = "dbo")]
  [CustomerValidator]
  public partial class Customer {

    #region Data Properties

    /// <summary>Gets or sets the CustomerID. </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("CustomerID")]
    public System.Guid CustomerID { get; set; }

    /// <summary>Gets or sets the CustomerID_OLD. </summary>
    [Column("CustomerID_OLD")]
    [MaxLength(5)]
    public string CustomerID_OLD { get; set; }

    /// <summary>Gets or sets the CompanyName. </summary>
    [Column("CompanyName")]
    [MaxLength(40)]
    [Required]
    public string CompanyName { get; set; }

    /// <summary>Gets or sets the ContactName. </summary>
    [Column("ContactName")]
    [CustomValidator]
    [MaxLength(30)]
    public string ContactName { get; set; }

    /// <summary>Gets or sets the ContactTitle. </summary>
    [Column("ContactTitle")]
    [MaxLength(30)]
    public string ContactTitle { get; set; }

    /// <summary>Gets or sets the Address. </summary>
    [Column("Address")]
    [MaxLength(60)]
    public string Address { get; set; }

    /// <summary>Gets or sets the City. </summary>
    [Column("City")]
    [MaxLength(15)]
    public string City { get; set; }

    /// <summary>Gets or sets the Region. </summary>
    [Column("Region")]
    [MaxLength(15)]
    public string Region { get; set; }

    /// <summary>Gets or sets the PostalCode. </summary>
    [Column("PostalCode")]
    [MaxLength(10)]
    public string PostalCode { get; set; }

    /// <summary>Gets or sets the Country. </summary>
    [Column("Country")]
    [MaxLength(15)]
    public string Country { get; set; }

    /// <summary>Gets or sets the Phone. </summary>
    [Column("Phone")]
    [MaxLength(24)]
    public string Phone { get; set; }

    /// <summary>Gets or sets the Fax. </summary>
    [Column("Fax")]
    [MaxLength(24)]
    public string Fax { get; set; }

    /// <summary>Gets or sets the RowVersion. </summary>
    [Column("RowVersion")]
    [ConcurrencyCheck]
    public System.Nullable<int> RowVersion { get; set; }

    #endregion Data Properties

    #region Navigation properties

    /// <summary>Gets the Orders. </summary>
    [InverseProperty("Customer")]
    public ICollection<Order> Orders { get; set; }

    #endregion Navigation properties


  }

  #endregion Customer class

  #region Employee class

  [Table("Employee", Schema = "dbo")]
  public partial class Employee {

    #region Data Properties

    /// <summary>Gets or sets the EmployeeID. </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("EmployeeID")]
    public int EmployeeID { get; set; }

    /// <summary>Gets or sets the LastName. </summary>
    [Column("LastName")]
    [MaxLength(30)]
    [Required]
    public string LastName { get; set; }


    [Column("FirstName")]
    [MaxLength(30)]
    [Required]
    public string FirstName { get; set; }

    [Column("Title")]
    [MaxLength(30)]
    public string Title { get; set; }

    [Column("TitleOfCourtesy")]
    [MaxLength(25)]
    public string TitleOfCourtesy { get; set; }

    [Column("BirthDate")]
    public System.Nullable<System.DateTime> BirthDate { get; set; }

    [Column("HireDate")]
    public System.Nullable<System.DateTime> HireDate { get; set; }

    [Column("Address")]
    [MaxLength(60)]
    public string Address { get; set; }

    [Column("City")]
    [MaxLength(15)]
    public string City { get; set; }

    [Column("Region")]
    [MaxLength(15)]
    public string Region { get; set; }

    [Column("PostalCode")]
    [MaxLength(10)]
    public string PostalCode { get; set; }

    [Column("Country")]
    [MaxLength(15)]
    public string Country { get; set; }

    [Column("HomePhone")]
    [MaxLength(24)]
    public string HomePhone { get; set; }

    [Column("Extension")]
    [MaxLength(4)]
    public string Extension { get; set; }

    [Column("Photo")]
    public byte[] Photo { get; set; }

    [Column("Notes")]
    public string Notes { get; set; }

    [Column("PhotoPath")]
    [MaxLength(255)]
    public string PhotoPath { get; set; }

    [Column("ReportsToEmployeeID")]
    public System.Nullable<int> ReportsToEmployeeID { get; set; }

    [Column("RowVersion")]
    public int RowVersion { get; set; }

    [Column("FullName")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public String FullName { get; set; }

    #endregion Data Properties

    #region Navigation properties

    [InverseProperty("Manager")]
    public ICollection<Employee> DirectReports { get; set; }

    [ForeignKey("ReportsToEmployeeID")]
    [InverseProperty("DirectReports")]
    public Employee Manager { get; set; }

    [InverseProperty("Employee")]
    public ICollection<EmployeeTerritory> EmployeeTerritories { get; set; }

    [InverseProperty("Employee")]
    public ICollection<Order> Orders { get; set; }

    //[InverseProperty("Employees")]
    //public ICollection<Territory> Territories { get; set; }

    #endregion Navigation properties

  }

  #endregion Employee class

  #region EmployeeTerritory class

  [Table("EmployeeTerritory", Schema = "dbo")]
  public partial class EmployeeTerritory {

    #region Data Properties

    /// <summary>Gets or sets the ID. </summary>
    [Key]
    // [DataMember]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("ID")]
    // [IbVal.RequiredValueVerifier( ErrorMessageResourceName="EmployeeTerritory_ID")]
    public int ID { get; set; }

    /// <summary>Gets or sets the EmployeeID. </summary>
    // [DataMember]
    //[ForeignKey("Employee")]
    [Column("EmployeeID")]
    // [IbVal.RequiredValueVerifier( ErrorMessageResourceName="EmployeeTerritory_EmployeeID")]
    public int EmployeeID { get; set; }

    /// <summary>Gets or sets the TerritoryID. </summary>
    // [DataMember]
    // [ForeignKey("Territory")]
    [Column("TerritoryID")]
    // [IbVal.RequiredValueVerifier( ErrorMessageResourceName="EmployeeTerritory_TerritoryID")]
    public int TerritoryID { get; set; }

    /// <summary>Gets or sets the RowVersion. </summary>
    // [DataMember]
    [Column("RowVersion")]
    // [IbVal.RequiredValueVerifier( ErrorMessageResourceName="EmployeeTerritory_RowVersion")]
    public int RowVersion { get; set; }

    #endregion Data Properties

    #region Navigation properties

    /// <summary>Gets or sets the Employee. </summary>
    // [DataMember]
    [ForeignKey("EmployeeID")]
    [InverseProperty("EmployeeTerritories")]
    public Employee Employee { get; set; }

    /// <summary>Gets or sets the Territory. </summary>
    // [DataMember]
    [ForeignKey("TerritoryID")]
    [InverseProperty("EmployeeTerritories")]
    public Territory Territory { get; set; }

    #endregion Navigation properties

  }

  #endregion EmployeeTerritory class

  #region Order class

  [Table("Order", Schema = "dbo")]
  public partial class Order {

    #region Data Properties

    /// <summary>Gets or sets the OrderID. </summary>
    [Key]
    // [DataMember]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("OrderID")]
    public int OrderID { get; set; }

    /// <summary>Gets or sets the CustomerID. </summary>
    // [DataMember]
    // [ForeignKey("Customer")]
    [Column("CustomerID")]
    public System.Nullable<System.Guid> CustomerID { get; set; }

    /// <summary>Gets or sets the EmployeeID. </summary>
    // [DataMember]
    // [ForeignKey("Employee")]
    [Column("EmployeeID")]
    public System.Nullable<int> EmployeeID { get; set; }

    /// <summary>Gets or sets the OrderDate. </summary>
    // [DataMember]
    [Column("OrderDate")]
    public System.Nullable<System.DateTime> OrderDate { get; set; }

    /// <summary>Gets or sets the RequiredDate. </summary>
    // [DataMember]
    [Column("RequiredDate")]
    public System.Nullable<System.DateTime> RequiredDate { get; set; }

    /// <summary>Gets or sets the ShippedDate. </summary>
    // [DataMember]
    [Column("ShippedDate")]
    public System.Nullable<System.DateTime> ShippedDate { get; set; }

    /// <summary>Gets or sets the Freight. </summary>
    // [DataMember]
    [Column("Freight")]
    public System.Nullable<decimal> Freight { get; set; }

    /// <summary>Gets or sets the ShipName. </summary>
    // [DataMember]
    [Column("ShipName")]
    // [IbVal.StringLengthVerifier(MaxValue=40, IsRequired=false, ErrorMessageResourceName="Order_ShipName")]
    [MaxLength(40)]
    public string ShipName { get; set; }

    /// <summary>Gets or sets the ShipAddress. </summary>
    // [DataMember]
    [Column("ShipAddress")]
    // [IbVal.StringLengthVerifier(MaxValue=60, IsRequired=false, ErrorMessageResourceName="Order_ShipAddress")]
    [MaxLength(60)]
    public string ShipAddress { get; set; }

    /// <summary>Gets or sets the ShipCity. </summary>
    // [DataMember]
    [Column("ShipCity")]
    // [IbVal.StringLengthVerifier(MaxValue=15, IsRequired=false, ErrorMessageResourceName="Order_ShipCity")]
    [MaxLength(15)]
    public string ShipCity { get; set; }

    /// <summary>Gets or sets the ShipRegion. </summary>
    // [DataMember]
    [Column("ShipRegion")]
    // [IbVal.StringLengthVerifier(MaxValue=15, IsRequired=false, ErrorMessageResourceName="Order_ShipRegion")]
    [MaxLength(15)]
    public string ShipRegion { get; set; }

    /// <summary>Gets or sets the ShipPostalCode. </summary>
    // [DataMember]
    [Column("ShipPostalCode")]
    // [IbVal.StringLengthVerifier(MaxValue=10, IsRequired=false, ErrorMessageResourceName="Order_ShipPostalCode")]
    [MaxLength(10)]
    public string ShipPostalCode { get; set; }

    /// <summary>Gets or sets the ShipCountry. </summary>
    // [DataMember]
    [Column("ShipCountry")]
    // [IbVal.StringLengthVerifier(MaxValue=15, IsRequired=false, ErrorMessageResourceName="Order_ShipCountry")]
    [MaxLength(15)]
    public string ShipCountry { get; set; }

    /// <summary>Gets or sets the RowVersion. </summary>
    // [DataMember]
    [Column("RowVersion")]
    // [IbVal.RequiredValueVerifier( ErrorMessageResourceName="Order_RowVersion")]
    public int RowVersion { get; set; }

    #endregion Data Properties

    #region Navigation properties

    /// <summary>Gets or sets the Customer. </summary>
    // [DataMember]
    [ForeignKey("CustomerID")]
    [InverseProperty("Orders")]
    public Customer Customer { get; set; }

    /// <summary>Gets or sets the Employee. </summary>
    // [DataMember]
    [ForeignKey("EmployeeID")]
    [InverseProperty("Orders")]
    public Employee Employee { get; set; }

    /// <summary>Gets the OrderDetails. </summary>
    // [DataMember]
    [InverseProperty("Order")]
    public ICollection<OrderDetail> OrderDetails { get; set; }

    /// <summary>Gets or sets the InternationalOrder. </summary>
    // [DataMember]
    [InverseProperty("Order")]
    public InternationalOrder InternationalOrder { get; set; }

    #endregion Navigation properties

  }

  #endregion Order class

  #region OrderDetail class

  [Table("OrderDetail", Schema = "dbo")]
  public partial class OrderDetail {

    #region Data Properties

    /// <summary>Gets or sets the OrderID. </summary>
    // [Key]
    // [DatabaseGenerated(DatabaseGeneratedOption.None)]
    // // [DataMember]
    [Column("OrderID")]
    public int OrderID { get; set; }

    /// <summary>Gets or sets the ProductID. </summary>
    // [Key]
    // [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("ProductID")]
    // // [DataMember]
    public int ProductID { get; set; }

    /// <summary>Gets or sets the UnitPrice. </summary>  
    [Column("UnitPrice")]
    // // [DataMember]
    public decimal UnitPrice { get; set; }

    /// <summary>Gets or sets the Quantity. </summary>
    
    [Column("Quantity")]
    // // [DataMember]
    public short Quantity { get; set; }

    /// <summary>Gets or sets the Discount. </summary>
    [Column("Discount")]
    // // [DataMember]
    public float Discount { get; set; }

    /// <summary>Gets or sets the RowVersion. </summary>
    [Column("RowVersion")]
    // // [DataMember]
    public int RowVersion { get; set; }

    #endregion Data Properties

    #region Navigation properties

    /// <summary>Gets or sets the Order. </summary>
    // // [DataMember]
    [ForeignKey("OrderID")]
    [InverseProperty("OrderDetails")]
    public Order Order { get; set; }

    /// <summary>Gets or sets the Product. </summary>
    [ForeignKey("ProductID")]
    // [InverseProperty("OrderDetails")]
    // // [DataMember]
    public Product Product { get; set; }

    #endregion Navigation properties

  }

  #endregion OrderDetail class

  #region PreviousEmployee class

  [Table("PreviousEmployee", Schema = "dbo")]
  public partial class PreviousEmployee {

    #region Data Properties

    /// <summary>Gets or sets the EmployeeID. </summary>
    [Key]
    // [DataMember]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("EmployeeID")]
    // [IbVal.RequiredValueVerifier( ErrorMessageResourceName="PreviousEmployee_EmployeeID")]
    public int EmployeeID { get; set; }

    /// <summary>Gets or sets the LastName. </summary>
    // [DataMember]
    [Column("LastName")]
    // [IbVal.StringLengthVerifier(MaxValue=20, IsRequired=true, ErrorMessageResourceName="PreviousEmployee_LastName")]
    [MaxLength(20)]
    [Required]
    public string LastName { get; set; }

    /// <summary>Gets or sets the FirstName. </summary>
    // [DataMember]
    [Column("FirstName")]
    // [IbVal.StringLengthVerifier(MaxValue=10, IsRequired=true, ErrorMessageResourceName="PreviousEmployee_FirstName")]
    [MaxLength(10)]
    [Required]
    public string FirstName { get; set; }

    /// <summary>Gets or sets the Title. </summary>
    // [DataMember]
    [Column("Title")]
    // [IbVal.StringLengthVerifier(MaxValue=30, IsRequired=false, ErrorMessageResourceName="PreviousEmployee_Title")]
    [MaxLength(30)]
    public string Title { get; set; }

    /// <summary>Gets or sets the TitleOfCourtesy. </summary>
    // [DataMember]
    [Column("TitleOfCourtesy")]
    // [IbVal.StringLengthVerifier(MaxValue=25, IsRequired=false, ErrorMessageResourceName="PreviousEmployee_TitleOfCourtesy")]
    [MaxLength(25)]
    public string TitleOfCourtesy { get; set; }

    /// <summary>Gets or sets the BirthDate. </summary>
    // [DataMember]
    [Column("BirthDate")]
    public System.Nullable<System.DateTime> BirthDate { get; set; }

    /// <summary>Gets or sets the HireDate. </summary>
    // [DataMember]
    [Column("HireDate")]
    public System.Nullable<System.DateTime> HireDate { get; set; }

    /// <summary>Gets or sets the Address. </summary>
    // [DataMember]
    [Column("Address")]
    // [IbVal.StringLengthVerifier(MaxValue=60, IsRequired=false, ErrorMessageResourceName="PreviousEmployee_Address")]
    public string Address { get; set; }

    /// <summary>Gets or sets the City. </summary>
    // [DataMember]
    [Column("City")]
    // [IbVal.StringLengthVerifier(MaxValue=15, IsRequired=false, ErrorMessageResourceName="PreviousEmployee_City")]
    [MaxLength(15)]
    public string City { get; set; }

    /// <summary>Gets or sets the Region. </summary>
    // [DataMember]
    [Column("Region")]
    // [IbVal.StringLengthVerifier(MaxValue=15, IsRequired=false, ErrorMessageResourceName="PreviousEmployee_Region")]
    [MaxLength(15)]
    public string Region { get; set; }

    /// <summary>Gets or sets the PostalCode. </summary>
    // [DataMember]
    [Column("PostalCode")]
    // [IbVal.StringLengthVerifier(MaxValue=10, IsRequired=false, ErrorMessageResourceName="PreviousEmployee_PostalCode")]
    [MaxLength(10)]
    public string PostalCode { get; set; }

    /// <summary>Gets or sets the Country. </summary>
    // [DataMember]
    [Column("Country")]
    // [IbVal.StringLengthVerifier(MaxValue=15, IsRequired=false, ErrorMessageResourceName="PreviousEmployee_Country")]
    [MaxLength(15)]
    public string Country { get; set; }

    /// <summary>Gets or sets the HomePhone. </summary>
    // [DataMember]
    [Column("HomePhone")]
    // [IbVal.StringLengthVerifier(MaxValue=24, IsRequired=false, ErrorMessageResourceName="PreviousEmployee_HomePhone")]
    [MaxLength(24)]
    public string HomePhone { get; set; }

    /// <summary>Gets or sets the Extension. </summary>
    // [DataMember]
    [Column("Extension")]
    // [IbVal.StringLengthVerifier(MaxValue=4, IsRequired=false, ErrorMessageResourceName="PreviousEmployee_Extension")]
    [MaxLength(4)]
    public string Extension { get; set; }

    /// <summary>Gets or sets the Photo. </summary>
    // [DataMember]
    [Column("Photo")]
    public byte[] Photo { get; set; }

    /// <summary>Gets or sets the Notes. </summary>
    // [DataMember]
    [Column("Notes")]
    public string Notes { get; set; }

    /// <summary>Gets or sets the PhotoPath. </summary>
    // [DataMember]
    [Column("PhotoPath")]
    // [IbVal.StringLengthVerifier(MaxValue=255, IsRequired=false, ErrorMessageResourceName="PreviousEmployee_PhotoPath")]
    [MaxLength(255)]
    public string PhotoPath { get; set; }

    /// <summary>Gets or sets the RowVersion. </summary>
    // [DataMember]
    [Column("RowVersion")]
    // [IbVal.RequiredValueVerifier( ErrorMessageResourceName="PreviousEmployee_RowVersion")]
    public int RowVersion { get; set; }

    #endregion Data Properties

    #region Navigation properties

    #endregion Navigation properties

  }

  #endregion PreviousEmployee class

  #region Product class

  [Table("Product", Schema = "dbo")]
  public partial class Product {

    #region Data Properties

    /// <summary>Gets or sets the ProductID. </summary>
    [Key]
    // [DataMember]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("ProductID")]
    // [IbVal.RequiredValueVerifier( ErrorMessageResourceName="Product_ProductID")]
    public int ProductID { get; set; }

    /// <summary>Gets or sets the ProductName. </summary>
    // [DataMember]
    [Column("ProductName")]
    // [IbVal.StringLengthVerifier(MaxValue=40, IsRequired=true, ErrorMessageResourceName="Product_ProductName")]
    [MaxLength(40)]
    public string ProductName { get; set; }

    /// <summary>Gets or sets the SupplierID. </summary>
    // [DataMember]
    //[ForeignKey("Supplier")]
    [Column("SupplierID")]
    public System.Nullable<int> SupplierID { get; set; }

    /// <summary>Gets or sets the CategoryID. </summary>
    // [DataMember]
    // [ForeignKey("Category")]
    [Column("CategoryID")]
    public System.Nullable<int> CategoryID { get; set; }

    /// <summary>Gets or sets the QuantityPerUnit. </summary>
    // [DataMember]
    [Column("QuantityPerUnit")]
    // [IbVal.StringLengthVerifier(MaxValue=20, IsRequired=false, ErrorMessageResourceName="Product_QuantityPerUnit")]
    public string QuantityPerUnit { get; set; }

    /// <summary>Gets or sets the UnitPrice. </summary>
    // [DataMember]
    [Column("UnitPrice")]
    public System.Nullable<decimal> UnitPrice { get; set; }

    /// <summary>Gets or sets the UnitsInStock. </summary>
    // [DataMember]
    [Column("UnitsInStock")]
    public System.Nullable<short> UnitsInStock { get; set; }

    /// <summary>Gets or sets the UnitsOnOrder. </summary>
    // [DataMember]
    [Column("UnitsOnOrder")]
    public System.Nullable<short> UnitsOnOrder { get; set; }

    /// <summary>Gets or sets the ReorderLevel. </summary>
    // [DataMember]
    [Column("ReorderLevel")]
    public System.Nullable<short> ReorderLevel { get; set; }

    /// <summary>Gets or sets the Discontinued. </summary>
    // [DataMember]
    [Column("Discontinued")]
    [DefaultValue(false)]
    // [IbVal.RequiredValueVerifier( ErrorMessageResourceName="Product_Discontinued")]
    public bool Discontinued { get; set; }

    /// <summary>Gets or sets the DiscontinuedDate. </summary>
    // [DataMember]
    [Column("DiscontinuedDate")]
    public System.Nullable<System.DateTime> DiscontinuedDate { get; set; }

    /// <summary>Gets or sets the RowVersion. </summary>
    // [DataMember]
    [Column("RowVersion")]
    // [IbVal.RequiredValueVerifier( ErrorMessageResourceName="Product_RowVersion")]
    public int RowVersion { get; set; }

    #endregion Data Properties

    #region Navigation properties

    /// <summary>Gets or sets the Category. </summary>
    // [DataMember]
    [ForeignKey("CategoryID")]
    [InverseProperty("Products")]
    public Category Category { get; set; }

    ///// <summary>Gets the OrderDetails. </summary>
    //// [DataMember]
    //[InverseProperty("Product")]
    //public ICollection<OrderDetail> OrderDetails {
    //  get;
    //  set;
    //}

    /// <summary>Gets or sets the Supplier. </summary>
    // [DataMember]
    [ForeignKey("SupplierID")]
    [InverseProperty("Products")]
    public Supplier Supplier { get; set; }

    #endregion Navigation properties

  }

  #endregion Product class

  #region Region class

  [Table("Region", Schema = "dbo")]
  public partial class Region {

    #region Data Properties

    /// <summary>Gets or sets the RegionID. </summary>
    [Key]
    // [DataMember]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("RegionID")]
    // [IbVal.RequiredValueVerifier( ErrorMessageResourceName="Region_RegionID")]
    public int RegionID { get; set; }

    /// <summary>Gets or sets the RegionDescription. </summary>
    // [DataMember]
    [Column("RegionDescription")]
    [MaxLength(50)]
    [Required]
    // [IbVal.StringLengthVerifier(MaxValue=50, IsRequired=true, ErrorMessageResourceName="Region_RegionDescription")]
    public string RegionDescription { get; set; }

    /// <summary>Gets or sets the RowVersion. </summary>
    // [DataMember]
    [Column("RowVersion")]
    // [IbVal.RequiredValueVerifier( ErrorMessageResourceName="Region_RowVersion")]
    public int RowVersion { get; set; }

    #endregion Data Properties

    #region Navigation properties

    /// <summary>Gets the Territories. </summary>
    // [DataMember]
    [InverseProperty("Region")]
    public ICollection<Territory> Territories { get; set; }

    #endregion Navigation properties

  }

  #endregion Region class

  #region Role class

  [Table("Role", Schema = "dbo")]
  public partial class Role {

    #region Data Properties

    /// <summary>Gets or sets the Id. </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("Id")]
    public long Id { get; set; }

    /// <summary>Gets or sets the Name. </summary>
    [Column("Name")]
    [MaxLength(50)]
    [Required]
    public string Name { get; set; }

    /// <summary>Gets or sets the Description. </summary>
    [Column("Description")]
    [MaxLength(2000)]
    public string Description { get; set; }


    [Column("Ts")]
    // [Required]
    [Timestamp]
    public byte[] Ts { get; set; }


    [Column("RoleType")]
    public Nullable<Models.NorthwindIB.RoleType> RoleType { get; set; }

    #endregion Data Properties

    #region Navigation properties

    /// <summary>Gets the UserRoles. </summary>
    [InverseProperty("Role")]
    public ICollection<UserRole> UserRoles { get; set; }

    #endregion Navigation properties

  }

  #endregion Role class

  #region Supplier class

  [Table("Supplier", Schema = "dbo")]
  public partial class Supplier {

    #region Data Properties

    /// <summary>Gets or sets the SupplierID. </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("SupplierID")]
    public int SupplierID { get; set; }

    /// <summary>Gets or sets the CompanyName. </summary>
    [Column("CompanyName")]
    [MaxLength(40)]
    [Required]
    public string CompanyName { get; set; }

    /// <summary>Gets or sets the ContactName. </summary>
    [Column("ContactName")]
    [MaxLength(30)]
    public string ContactName { get; set; }

    /// <summary>Gets or sets the ContactTitle. </summary>
    [Column("ContactTitle")]
    [MaxLength(30)]
    public string ContactTitle { get; set; }

    // [DataMember]
    public Location Location { get; set; }

    ///// <summary>Gets or sets the Address. </summary>
    //// [DataMember]
    //[Column("Address")]
    //// [IbVal.StringLengthVerifier(MaxValue=60, IsRequired=false, ErrorMessageResourceName="Supplier_Address")]
    //[MaxLength(60)]
    //public string Address {
    //  get;
    //  set;
    //}

    ///// <summary>Gets or sets the City. </summary>
    //// [DataMember]
    //[Column("City")]
    //// [IbVal.StringLengthVerifier(MaxValue=15, IsRequired=false, ErrorMessageResourceName="Supplier_City")]
    //[MaxLength(15)]
    //public string City {
    //  get;
    //  set;
    //}

    ///// <summary>Gets or sets the Region. </summary>
    //// [DataMember]
    //[Column("Region")]
    //// [IbVal.StringLengthVerifier(MaxValue=15, IsRequired=false, ErrorMessageResourceName="Supplier_Region")]
    //[MaxLength(15)]
    //public string Region {
    //  get;
    //  set;
    //}

    ///// <summary>Gets or sets the PostalCode. </summary>
    //// [DataMember]
    //[Column("PostalCode")]
    //// [IbVal.StringLengthVerifier(MaxValue=10, IsRequired=false, ErrorMessageResourceName="Supplier_PostalCode")]
    //[MaxLength(10)]
    //public string PostalCode {
    //  get;
    //  set;
    //}

    ///// <summary>Gets or sets the Country. </summary>
    //// [DataMember]
    //[Column("Country")]
    //// [IbVal.StringLengthVerifier(MaxValue=15, IsRequired=false, ErrorMessageResourceName="Supplier_Country")]
    //[MaxLength(15)]
    //public string Country {
    //  get;
    //  set;
    //}

    /// <summary>Gets or sets the Phone. </summary>
    [Column("Phone")]
    [MaxLength(24)]
    public string Phone { get; set; }

    /// <summary>Gets or sets the Fax. </summary>
    [Column("Fax")]
    [MaxLength(24)]
    public string Fax { get; set; }

    /// <summary>Gets or sets the HomePage. </summary>
    [Column("HomePage")]
    public string HomePage { get; set; }

    /// <summary>Gets or sets the RowVersion. </summary>
    [Column("RowVersion")]
    public int RowVersion { get; set; }

    #endregion Data Properties

    #region Navigation properties

    /// <summary>Gets the Products. </summary>
    // [DataMember]
    [InverseProperty("Supplier")]
    public ICollection<Product> Products { get; set; }

    #endregion Navigation properties
  }

  #endregion Supplier class

  #region Location class

  
  public partial class Location {
    /// <summary>Gets or sets the Address. </summary>
  
    [Column("Address")]
    [MaxLength(60)]
    public string Address { get; set; }

    /// <summary>Gets or sets the City. </summary>
    [Column("City")]
    [MaxLength(15)]
    public string City { get; set; }

    /// <summary>Gets or sets the Region. </summary>
    // [DataMember]
    [Column("Region")]
    [MaxLength(15)]
    public string Region { get; set; }

    /// <summary>Gets or sets the PostalCode. </summary>
    [Column("PostalCode")]
    [MaxLength(10)]
    public string PostalCode { get; set; }

    /// <summary>Gets or sets the Country. </summary>
    [Column("Country")]
    [MaxLength(15)]
    public string Country { get; set; }
  }

  #endregion Location

  #region Territory class

  [Table("Territory", Schema = "dbo")]
  public partial class Territory {

    #region Data Properties

    /// <summary>Gets or sets the TerritoryID. </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("TerritoryID")]
    public int TerritoryID { get; set; }

    /// <summary>Gets or sets the TerritoryDescription. </summary>
    [Column("TerritoryDescription")]
    [MaxLength(50)]
    [Required]
    public string TerritoryDescription { get; set; }

    /// <summary>Gets or sets the RegionID. </summary>
    // [ForeignKey("Region")]
    [Column("RegionID")]
    public int RegionID { get; set; }

    /// <summary>Gets or sets the RowVersion. </summary>
    [Column("RowVersion")]
    public int RowVersion { get; set; }

    #endregion Data Properties

    #region Navigation properties

    /// <summary>Gets the EmployeeTerritories. </summary>

    [InverseProperty("Territory")]
    public ICollection<EmployeeTerritory> EmployeeTerritories { get; set; }

    /// <summary>Gets or sets the Region. </summary>
    [ForeignKey("RegionID")]
    [InverseProperty("Territories")]
    public Region Region { get; set; }


    #endregion Navigation properties

  }

  #endregion Territory class

  #region User class

  [Table("User", Schema = "dbo")]
  public partial class User {

    #region Data Properties

    /// <summary>Gets or sets the Id. </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("Id")]
    public long Id { get; set; }

    /// <summary>Gets or sets the UserName. </summary>
    [Column("UserName")]
    [MaxLength(100)]
    public string UserName { get; set; }

    /// <summary>Gets or sets the UserPassword. </summary>
    [Column("UserPassword")]
    [MaxLength(200)]
    public string UserPassword { get; set; }

    /// <summary>Gets or sets the FirstName. </summary>
    [Column("FirstName")]
    [MaxLength(100)]
    public string FirstName { get; set; }

    /// <summary>Gets or sets the LastName. </summary>
    [Column("LastName")]
    [MaxLength(100)]
    public string LastName { get; set; }

    /// <summary>Gets or sets the Email. </summary>
    [Column("Email")]
    [MaxLength(100)]
    public string Email { get; set; }

    /// <summary>Gets or sets the RowVersion. </summary>
    [Column("RowVersion")]
    [ConcurrencyCheck]
    public decimal RowVersion { get; set; }

    /// <summary>Gets or sets the CreatedBy. </summary>
    [Column("CreatedBy")]
    [MaxLength(100)]
    public string CreatedBy { get; set; }

    /// <summary>Gets or sets the CreatedByUserId. </summary>
    [Column("CreatedByUserId")]
    public long CreatedByUserId { get; set; }

    /// <summary>Gets or sets the CreatedDate. </summary>
    [Column("CreatedDate")]
    public System.DateTime CreatedDate { get; set; }

    /// <summary>Gets or sets the ModifiedBy. </summary>
    [Column("ModifiedBy")]
    [MaxLength(100)]
    public string ModifiedBy { get; set; }

    /// <summary>Gets or sets the ModifiedByUserId. </summary>
    [Column("ModifiedByUserId")]
    public long ModifiedByUserId { get; set; }

    /// <summary>Gets or sets the ModifiedDate. </summary>
    [Column("ModifiedDate")]
    public System.DateTime ModifiedDate { get; set; }

    #endregion Data Properties

    #region Navigation properties

    /// <summary>Gets the UserRoles. </summary>
    [InverseProperty("User")]
    public ICollection<UserRole> UserRoles { get; set; }

    #endregion Navigation properties

  }

  #endregion User class

  #region UserRole class


  [Table("UserRole", Schema = "dbo")]
  public partial class UserRole {

    #region Data Properties

    /// <summary>Gets or sets the ID. </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("ID")]
    public long ID { get; set; }

    /// <summary>Gets or sets the UserId. </summary>
    // [ForeignKey("User")]
    [Column("UserId")]
    public long UserId { get; set; }

    /// <summary>Gets or sets the RoleId. </summary>
    // [ForeignKey("Role")]
    [Column("RoleId")]
    public long RoleId { get; set; }

    #endregion Data Properties

    #region Navigation properties

    /// <summary>Gets or sets the Role. </summary>
    [ForeignKey("RoleId")]
    [InverseProperty("UserRoles")]
    public Role Role { get; set; }

    /// <summary>Gets or sets the User. </summary>
    [ForeignKey("UserId")]
    [InverseProperty("UserRoles")]
    public User User { get; set; }

    #endregion Navigation properties

  }

  #endregion UserRole class

  #region InternationalOrder class

  [Table("InternationalOrder", Schema = "dbo")]
  public partial class InternationalOrder {

    #region Data Properties

    /// <summary>Gets or sets the OrderID. </summary>
    [Key]
    // [DataMember]
    [ForeignKey("Order")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("OrderID")]
    public int OrderID { get; set; }

    /// <summary>Gets or sets the CustomsDescription. </summary>
    // [DataMember]
    [Column("CustomsDescription")]
    [MaxLength(100)]
    public string CustomsDescription { get; set; }

    /// <summary>Gets or sets the ExciseTax. </summary>
    // [DataMember]
    [Column("ExciseTax")]
    public decimal ExciseTax { get; set; }

    /// <summary>Gets or sets the RowVersion. </summary>
    // [DataMember]
    [Column("RowVersion")]
    public int RowVersion { get; set; }

    #endregion Data Properties

    #region Navigation properties

    /// <summary>Gets or sets the Order. </summary>
    // [DataMember]
    //[ForeignKey("OrderID")]
    //[InverseProperty("InternationalOrder")]
    //[Required]
    public Order Order { get; set; }

    #endregion Navigation properties

  }

  #endregion InternationalOrder class

  #region TimeLimit class

  [Table("TimeLimit", Schema = "dbo")]
  public partial class TimeLimit {

    #region Data Properties
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("Id")]
    public int Id { get; set; }

    [Column("MaxTime")]
    [DefaultValue(0)]
    public System.TimeSpan MaxTime { get; set; }

    [Column("MinTime")]
    public Nullable<System.TimeSpan> MinTime { get; set; }

    // [ForeignKey("TimeGroup")]
    [Column("TimeGroupId")]
    public System.Nullable<int> TimeGroupId { get; set; }
    #endregion Data Properties

    #region Navigation Properties
    /// <summary>Gets or sets the TimeGroup. </summary>
    [ForeignKey("TimeGroupId")]
    [InverseProperty("TimeLimits")]
    public TimeGroup TimeGroup { get; set; }
    #endregion Navigation Properties
  }

  #endregion TimeLimit

  #region TimeGroup class

  // [DataContract(IsReference = true)]
  [Table("TimeGroup", Schema = "dbo")]
  public partial class TimeGroup {

    #region Data Properties
    [Key]
    // [DataMember]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("Id")]
    public int Id { get; set; }

    // [DataMember]
    [Column("Comment")]
    public string Comment { get; set; }
    #endregion Data Properties

    #region Navigation Properties
    // [DataMember]
    [InverseProperty("TimeGroup")]
    public ICollection<TimeLimit> TimeLimits { get; set; }
    #endregion Navigation Properties
  }

  #endregion TimeGroup

  #region Comment class

  // [DataContract(IsReference = true)]
  [Table("Comment", Schema = "dbo")]
  public partial class Comment {

    #region Data Properties

    [Key]
    // [DataMember]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("CreatedOn", Order = 1)]
    public System.DateTime CreatedOn { get; set; }

    [Key]
    // [DataMember]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("SeqNum", Order = 2)]
    public byte SeqNum { get; set; }

    // [DataMember]
    [Column("Comment1")]
    public string Comment1 { get; set; }

    #endregion Data Properties

    #region Navigation properties

    #endregion Navigation properties

  }
  #endregion Comment

//  #region Geospatial class

//  // [DataContract(IsReference = true)]
//  [Table("Geospatial", Schema = "dbo")]
//  public partial class Geospatial {

//    public Geospatial() {
//      this.Geometry1 = DbGeometry.FromText("POLYGON ((30 10, 10 20, 20 40, 40 40, 30 10))");
//      this.Geography1 = DbGeography.FromText("MULTIPOINT(-122.360 47.656, -122.343 47.656)", 4326);
//      // this.Geometry1 = DbGeometry.FromText("GEOMETRYCOLLECTION(POINT(4 6),LINESTRING(4 6,7 10)");
//    }

//    [Key]
//    // [DataMember]
//    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
//    [Column("Id")]
//    public int Id { get; set; }


//    // [DataMember]
//    [Column("Geometry1")]
//    public DbGeometry Geometry1 { get; set; }

//    // [DataMember]
//    [Column("Geography1")]
//    public DbGeography Geography1 { get; set; }

//  }

//  #endregion Geospatial

  #region UnusualDate class

  // [DataContract(IsReference = true)]
  [Table("UnusualDate", Schema = "dbo")]
  public partial class UnusualDate {

    [Key]
    // [DataMember]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("Id")]
    public int Id { get; set; }

    // [DataMember]
    [Column("CreationDate")]
    public System.DateTimeOffset CreationDate { get; set; }

    // [DataMember]
    [Column("ModificationDate")]
    public System.DateTime ModificationDate { get; set; }

    // [DataMember]
    [Column("CreationDate2")]
    public Nullable<System.DateTimeOffset> CreationDate2 { get; set; }

    // [DataMember]
    [Column("ModificationDate2")]
    public Nullable<System.DateTime> ModificationDate2 { get; set; }
  }

  #endregion UnusualDate
}

