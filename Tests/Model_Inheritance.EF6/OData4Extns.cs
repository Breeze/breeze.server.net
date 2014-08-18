using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;


namespace Inheritance.Models {

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


  public partial class BillingDetailTPH {

    [NotMapped]
    public DateTimeOffset OData4CreatedAt {
      get { return Utils.ToDTO(CreatedAt); }
      set { CreatedAt = Utils.ToDT(value); }
    }
  }

  public partial class BillingDetailTPT {

    [NotMapped]
    public DateTimeOffset OData4CreatedAt {
      get { return Utils.ToDTO(CreatedAt); }
      set { CreatedAt = Utils.ToDT(value); }
    }
  }

  public partial class BillingDetailTPC {

    [NotMapped]
    public DateTimeOffset OData4CreatedAt {
      get { return Utils.ToDTO(CreatedAt); }
      set { CreatedAt = Utils.ToDT(value); }
    }
  }

  public partial class DepositTPH {

    [NotMapped]
    public DateTimeOffset OData4Deposited {
      get { return Utils.ToDTO(Deposited); }
      set { Deposited = Utils.ToDT(value); }
    }
  }

  public partial class DepositTPT {

    [NotMapped]
    public DateTimeOffset OData4Deposited {
      get { return Utils.ToDTO(Deposited); }
      set { Deposited = Utils.ToDT(value); }
    }
  }

  public partial class DepositTPC {

    [NotMapped]
    public DateTimeOffset OData4Deposited {
      get { return Utils.ToDTO(Deposited); }
      set { Deposited = Utils.ToDT(value); }
    }
  }


}
