using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.NorthwindIB.EDMX_2012 {
  public partial class NorthwindIBContext_EDMX_2012 : DbContext {
    public NorthwindIBContext_EDMX_2012(DbConnection connection) :
      base(connection, false) {
      // Disable proxy creation as this messes up the data service.
      this.Configuration.ProxyCreationEnabled = false;
      this.Configuration.LazyLoadingEnabled = false;

      // Create Northwind if it doesn't already exist.
      //this.Database.CreateIfNotExists();
    }
  }
}