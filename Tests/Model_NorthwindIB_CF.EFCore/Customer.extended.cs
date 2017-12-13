using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Foo {
  public partial class Customer {
    [DataMember]
    [NotMapped]
    public string ExtraString {
      get { return _extraString; }
      set { _extraString = value; }
    }

    [DataMember]
    [NotMapped]
    public double ExtraDouble {
      get { return _extraDouble; }
      set { _extraDouble = value; }
    }

    private string _extraString = "fromServer";
    private double _extraDouble = 3.14159;
  }
}
