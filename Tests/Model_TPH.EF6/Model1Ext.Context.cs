using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Extension class - needed for Asp.NET core...

namespace ProduceTPH {
  using System;
  using System.Data.Entity;
  using System.Data.Entity.Infrastructure;

  public partial class ProduceTPHContext : DbContext {

    public ProduceTPHContext(string connectionString) : base(connectionString) {
      // next line is just so that we can add a breakpoint there.
      var x = 3;
    }
  }
}