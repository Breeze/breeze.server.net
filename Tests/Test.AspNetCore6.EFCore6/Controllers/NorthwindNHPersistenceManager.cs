#if NHIBERNATE
using Breeze.Persistence;
using Breeze.Persistence.NH;
using Models.NorthwindIB.NH;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Test.AspNetCore.Controllers {

  /// <summary> Wrap an ISessionFactory so we can give it a unique type for DI </summary>
  public class NHSessionProvider<T> {
    private readonly ISessionFactory factory;
    public NHSessionProvider(ISessionFactory factory) {
      this.factory = factory;
    }
    public ISession OpenSession() { return factory.OpenSession(); }
  }

  /// <summary> NHPersistenceManager with validation added.  TODO should this be in the base implementation? </summary>
  public class ValidatingPersistenceManager : NHPersistenceManager {
    public ValidatingPersistenceManager(ISession session) : base(session) { }

    public ValidatingPersistenceManager(NHPersistenceManager source) : base(source) { }

    protected override Dictionary<Type, List<EntityInfo>> BeforeSaveEntities(Dictionary<Type, List<EntityInfo>> saveMap) {
      var meta = this.GetMetadata();
      var bvalidator = new BreezeEntityValidator(this, meta);
      bvalidator.ValidateEntities(saveMap, true);

      //DataAnnotationsValidator.AddDescriptor(typeof(Customer), typeof(CustomerMetaData));
      var validator = new DataAnnotationsValidator(this);
      validator.ValidateEntities(saveMap, true);

      return base.BeforeSaveEntities(saveMap);
    }
  }

  public class NorthwindNHPersistenceManager : ValidatingPersistenceManager {
    //public NorthwindNHPersistenceManager(ISessionFactory sessionFactory) : base(sessionFactory.OpenSession()) { }
    public NorthwindNHPersistenceManager(ISession session) : base(session) { }

    public NorthwindNHPersistenceManager(NHPersistenceManager source) : base(source) { }

    public NorthwindNHPersistenceManager Context {
      get { return this; }
    }
    public IQueryable<Category> Categories {
      get { return GetQuery<Category>(); }
    }
    public IQueryable<Comment> Comments {
      get { return GetQuery<Comment>(); }
    }
    public IQueryable<Customer> Customers {
      get { return GetQuery<Customer>(); }
    }
    public IQueryable<Employee> Employees {
      get { return GetQuery<Employee>(); }
    }
    //public IQueryable<Geospatial> Geospatials {
    //  get { return GetQuery<Geospatial>(); }
    //}
    public IQueryable<Order> Orders {
      get { return GetQuery<Order>(); }
    }
    public IQueryable<OrderDetail> OrderDetails {
      get { return GetQuery<OrderDetail>(); }
    }
    public IQueryable<Product> Products {
      get { return GetQuery<Product>(); }
    }
    public IQueryable<Region> Regions {
      get { return GetQuery<Region>(); }
    }
    public IQueryable<Role> Roles {
      get { return GetQuery<Role>(); }
    }
    public IQueryable<Supplier> Suppliers {
      get { return GetQuery<Supplier>(); }
    }
    public IQueryable<Territory> Territories {
      get { return GetQuery<Territory>(); }
    }
    public IQueryable<TimeGroup> TimeGroups {
      get { return GetQuery<TimeGroup>(); }
    }
    public IQueryable<TimeLimit> TimeLimits {
      get { return GetQuery<TimeLimit>(); }
    }
    public IQueryable<UnusualDate> UnusualDates {
      get { return GetQuery<UnusualDate>(); }
    }
    public IQueryable<User> Users {
      get { return GetQuery<User>(); }
    }

  }

}
#endif
