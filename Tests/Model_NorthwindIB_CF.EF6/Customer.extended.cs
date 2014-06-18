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
    public string ExtraInfo {
      get { return _extraInfo; }
      set { _extraInfo = value; }
    }

    private string _extraInfo = "fromServer";
  }
}
