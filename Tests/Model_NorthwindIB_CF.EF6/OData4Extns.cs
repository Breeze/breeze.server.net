using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;


namespace Foo {

  public static class Utils {
    public static DateTimeOffset ToDTO(DateTime dt) {
      return new DateTimeOffset(dt);
    }

    public static DateTimeOffset? ToDTO(DateTime? dt) {
      return dt == null ? (DateTimeOffset?) null : new DateTimeOffset(dt.Value);
    }

    public static DateTime ToDT(DateTimeOffset dto) {
      return dto.DateTime;
    }

    public static DateTime? ToDT(DateTimeOffset? dto) {
      return dto == null ? (DateTime?) null : dto.Value.DateTime;
    }
  }


  public partial class Order {

    [NotMapped]
    public System.Nullable<System.DateTimeOffset> OData4OrderDate {
      get { return Utils.ToDTO(OrderDate); }
      set { OrderDate = Utils.ToDT(value); }
    }

    [NotMapped]
    public System.Nullable<System.DateTimeOffset> OData4RequiredDate {
      get { return Utils.ToDTO(RequiredDate); }
      set { RequiredDate = Utils.ToDT(value); }
    }

    [NotMapped]
    public System.Nullable<System.DateTimeOffset> OData4ShippedDate {
      get { return Utils.ToDTO(ShippedDate); }
      set { ShippedDate = Utils.ToDT(value); }
    }
  }

  public partial class Employee {

    [NotMapped]
    public System.Nullable<System.DateTimeOffset> OData4BirthDate {
      get { return Utils.ToDTO(BirthDate); }
      set { BirthDate = Utils.ToDT(value); }
    }

    [NotMapped]
    public System.Nullable<System.DateTimeOffset> OData4HireDate {
      get { return Utils.ToDTO(HireDate); }
      set { HireDate = Utils.ToDT(value); }
    }
  }

  public partial class PreviousEmployee {

    [NotMapped]
    public System.Nullable<System.DateTimeOffset> OData4BirthDate {
      get { return Utils.ToDTO(BirthDate); }
      set { BirthDate = Utils.ToDT(value); }
    }

    [NotMapped]
    public System.Nullable<System.DateTimeOffset> OData4HireDate {
      get { return Utils.ToDTO(HireDate); }
      set { HireDate = Utils.ToDT(value); }
    }
  }

  public partial class Product {

    [NotMapped]
    public System.Nullable<System.DateTimeOffset> OData4DiscontinuedDate {
      get { return Utils.ToDTO(DiscontinuedDate); }
      set { DiscontinuedDate = Utils.ToDT(value); }
    }

  }

  public partial class User {

    [NotMapped]
    public System.DateTimeOffset OData4CreatedDate {
      get { return Utils.ToDTO(CreatedDate); }
      set { CreatedDate = Utils.ToDT(value); }
    }

    [NotMapped]
    public System.DateTimeOffset OData4ModifiedDate {
      get { return Utils.ToDTO(ModifiedDate); }
      set { ModifiedDate = Utils.ToDT(value); }
    }
  }

  public partial class Comment {

    [NotMapped]
    public System.DateTimeOffset OData4CreatedOn {
      get { return Utils.ToDTO(CreatedOn); }
      set { CreatedOn = Utils.ToDT(value); }
    }
  }

  public partial class UnusualDate {

    [NotMapped]
    public System.DateTimeOffset OData4ModificationDate {
      get { return Utils.ToDTO(ModificationDate); }
      set { ModificationDate = Utils.ToDT(value); }
    }

    [NotMapped]
    public System.Nullable<System.DateTimeOffset> OData4ModificationDate2 {
      get { return Utils.ToDTO(ModificationDate2); }
      set { ModificationDate2 = Utils.ToDT(value); }
    }
  }
}
