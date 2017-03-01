(function (testFns) {
  if (testFns.DEBUG_ODATA || testFns.DEBUG_MONGO || testFns.DEBUG_SEQUELIZE || testFns.DEBUG_HIBERNATE) {
    module("query - Non EF", {});
    QUnit.skip("Skipping tests for OData/Mongo/Sequelize/Hibernate", function () {
      
    });
    return;
  };

  var breeze = testFns.breeze;
  var core = breeze.core;
  var Event = core.Event;
  var EntityType = breeze.EntityType;
  var NamingConvention = breeze.NamingConvention;
  var DataProperty = breeze.DataProperty;
  var DataService = breeze.DataService;
  var NavigationProperty = breeze.NavigationProperty;
  var DataType = breeze.DataType;
  var EntityQuery = breeze.EntityQuery;
  var MetadataStore = breeze.MetadataStore;
  var EntityManager = breeze.EntityManager;
  var EntityKey = breeze.EntityKey;
  var FilterQueryOp = breeze.FilterQueryOp;
  var Predicate = breeze.Predicate;
  var QueryOptions = breeze.QueryOptions;
  var FetchStrategy = breeze.FetchStrategy;
  var MergeStrategy = breeze.MergeStrategy;

  module("query - non EF", {
    beforeEach: function (assert) {
      testFns.setup(assert);
    },
    afterEach: function (assert) {

    }
  });


  function newAltEm() {
    var altServiceName = "breeze/NonEFModel";

    var dataService = new DataService({
      serviceName: altServiceName,
      hasServerMetadata: false
    });
    var altMs = new MetadataStore({
      namingConvention: NamingConvention.camelCase
    });

    return new EntityManager({
      dataService: dataService,
      metadataStore: altMs
    });
  }

  test("canValidateNonscalarComplexProps 1", function () {
    var em = newAltEm();

    initMsForPersonMeal(em.metadataStore);
    var shiftType = em.metadataStore.getEntityType("Shift");
    var person = em.createEntity("Person", { personId: 1 });
    var shifts = person.getProperty("shifts");
    var shift1 = shiftType.createInstance({ startDate: new Date(2010, 1, 1, 10, 30), numHours: 8 });
    shifts.push(shift1);
    var shift2 = shiftType.createInstance({ startDate: "Foo", numHours: 8 });
    shifts.push(shift2);
    person.entityAspect.acceptChanges();
    var valOk = person.entityAspect.validateEntity();
    ok(valOk == false, "should fail validation");
    var ves = person.entityAspect.getValidationErrors();
    ok(ves.length == 1 && ves[0].propertyName === "shifts.startDate" && ves[0].context.index == 1);
  });

  test("canSaveNonscalarComplexProps 1", function () {
    var em = newAltEm();

    initMsForPersonMeal(em.metadataStore);
    var shiftType = em.metadataStore.getEntityType("Shift");
    var person = em.createEntity("Person", { personId: 1 });
    var shifts = person.getProperty("shifts");
    var shift1 = shiftType.createInstance({ startDate: new Date(2010, 1, 1, 10, 30), numHours: 8 });
    shifts.push(shift1);
    person.entityAspect.acceptChanges();
    person.setProperty("firstName", "Albert");

    var shift2 = shiftType.createInstance({ startDate: new Date(2010, 1, 1, 10, 30), numHours: 8 });
    shifts.push(shift2);
    var helper = em.helper;
    var changedJson = helper.unwrapChangedValues(person, em.metadataStore);
    ok(changedJson.FirstName == "Albert");
    ok(changedJson.Shifts.length == 2);

  });

  test("canSaveNonscalarComplexProps 2", function () {
    var em = newAltEm();

    initMsForPersonMeal(em.metadataStore);
    var shiftType = em.metadataStore.getEntityType("Shift");
    var person = em.createEntity("Person", { personId: 1 });
    var shifts = person.getProperty("shifts");
    var shift1 = shiftType.createInstance({ startDate: new Date(2010, 1, 1, 10, 30), numHours: 8 });
    shifts.push(shift1);
    person.entityAspect.acceptChanges();
    person.setProperty("firstName", "Albert");
    shift1.setProperty("numHours", 7);
    var helper = em.helper;
    var changedJson = helper.unwrapChangedValues(person, em.metadataStore);
    ok(changedJson.FirstName == "Albert");
    ok(changedJson.Shifts.length == 1);

  });

  test("canSaveNonscalarComplexProps 3", function () {
    var em = newAltEm();

    initMsForPersonMeal(em.metadataStore);
    var shiftType = em.metadataStore.getEntityType("Shift");
    var person = em.createEntity("Person", { personId: 1 });
    var shifts = person.getProperty("shifts");
    var shift1 = shiftType.createInstance({ startDate: new Date(2010, 1, 1, 10, 30), numHours: 8 });
    shifts.push(shift1);
    person.entityAspect.acceptChanges();
    person.setProperty("firstName", "Albert");

    var helper = em.helper;
    var changedJson = helper.unwrapChangedValues(person, em.metadataStore);
    ok(changedJson.FirstName == "Albert");
    ok(changedJson.Shifts === undefined);

  });

  test("bad addEntityType - no key", function () {
    var ms = new MetadataStore();
    try {
      ms.addEntityType({
        shortName: "Person",
        namespace: testFns.sampleNamespace,
        dataProperties: {
          personId: { dataType: DataType.Int32, isNullable: false },
          firstName: { dataType: DataType.String, isNullable: false },
          lastName: { dataType: DataType.String, isNullable: false },
          birthDate: { dataType: DataType.DateTime }
        }
      });
      ok(false, "should not get here")
    } catch (e) {
      ok(e.message.toLowerCase().indexOf("ispartofkey") >= 0, "message should mention 'isPartOfKey'");
    }
  });

  test("create complexType - compact form", function () {
    var ms = new MetadataStore();
    try {
      ms.addEntityType({
        shortName: "Foo",
        namespace: testFns.sampleNamespace,
        isComplexType: true,
        dataProperties: {
          firstName: { dataType: DataType.String, isNullable: false },
          lastName: { dataType: DataType.String, isNullable: false },
          birthDate: { dataType: DataType.DateTime }
        }
      });
      ok(true, "should get here")
    } catch (e) {
      ok(false, "should not get here");
    }
  });


  test("getSimple - anonymous - Persons", function (assert) {
    var done = assert.async();
    var em = newAltEm();

    var query = breeze.EntityQuery.from("Persons");
    
    em.executeQuery(query).then(function (data) {
      ok(data.results.length > 0);
      var person = data.results[0];
      ok(person.meals.length > 0, "person should have meals");
      // deliberately omitted because we only use ids to link meals -> person
      // and fixup will not occur with anon types
      // ok(person.meals[0].person === person, "check internal consistency");
      var ents = em.getEntities();
      ok(ents.length === 0, "should return 0 - not yet entities");
    }).fail(testFns.handleFail).fin(done);

  });

  test("getSimple - typed - Persons", function (assert) {
    var done = assert.async();
    var em = newAltEm();

    initMsForPersonMeal(em.metadataStore);
    var query = breeze.EntityQuery.from("Persons");

    em.executeQuery(query).then(function (data) {
      ok(!em.hasChanges(), "should not have any changes");
      ok(data.results.length > 0);
      var person = data.results[0];
      var meals = person.getProperty("meals");
      ok(meals.length > 0, "person should have meals");
      ok(meals[0].getProperty("person") === person, "check internal consistency");
      var ents = em.getEntities();
      ok(ents.length > 0, "should return some entities");
    }).fail(testFns.handleFail).fin(done);

  });

  test("getSimple - typed - Persons - long form metadata", function (assert) {
    var done = assert.async();
    var em = newAltEm();

    initMsForPersonMeal_long(em.metadataStore);
    var query = breeze.EntityQuery.from("Persons");

    em.executeQuery(query).then(function (data) {
      ok(!em.hasChanges(), "should not have any changes");
      ok(data.results.length > 0);
      var person = data.results[0];
      var meals = person.getProperty("meals");
      ok(meals.length > 0, "person should have meals");
      ok(meals[0].getProperty("person") === person, "check internal consistency");
      var ents = em.getEntities();
      ok(ents.length > 0, "should return some entities");
    }).fail(testFns.handleFail).fin(done);

  });

  test("unattached children - inherit - 1", function () {
    var em = newAltEm();

    initMsForOrgBase(em.metadataStore);
    var org1 = em.createEntity("Organization", {
      id: 1,
      name: "Org1"
    });
    var ur1 = em.createEntity("UserRight", {
      id: 2,
      baseId: 1
    });
    var ur2 = em.createEntity("UserRight", {
      id: 3,
      baseId: 1
    });
    var orgRights = org1.getProperty("userRights");
    ok(orgRights.length === 2);
    ok(orgRights.some(function (ur) {
      return ur == ur1;
    }));
    ok(orgRights.some(function (ur) {
      return ur == ur2;
    }));
    var ur1Org = ur1.getProperty("base");
    ok(ur1Org === org1);
    var ur2Org = ur2.getProperty("base");
    ok(ur2Org === org1);
  });

  test("unattached children - inherit - 2", function () {
    var em = newAltEm();

    initMsForOrgBase(em.metadataStore);
    var ur1 = em.createEntity("UserRight", {
      id: 2,
      baseId: 1
    });
    var ur2 = em.createEntity("UserRight", {
      id: 3,
      baseId: 1
    });
    var org1 = em.createEntity("Organization", {
      id: 1,
      name: "Org1"
    });

    var orgRights = org1.getProperty("userRights");
    ok(orgRights.length === 2);
    ok(orgRights.some(function (ur) {
      return ur == ur1;
    }));
    ok(orgRights.some(function (ur) {
      return ur == ur2;
    }));
    var ur1Org = ur1.getProperty("base");
    ok(ur1Org === org1);
    var ur2Org = ur2.getProperty("base");
    ok(ur2Org === org1);
  });

  //    class Base {
  //        public int Id;
  //        virtual public ICollection<UserRight> UserRights;
  //    }
  //    class Organisation : Base {
  //        public String Name;
  //    }
  //    class UserRight {
  //        public int Id;
  //        public int BaseId;
  //        virtual public Base Base;
  //    }

  function initMsForOrgBase(metadataStore) {
    var ns = "XXX";
    var qt = function (shortName) {
      return EntityType.qualifyTypeName(shortName, ns);
    }
    var baseT = metadataStore.addEntityType({
      shortName: "Base",
      namespace: ns,
      dataProperties: {
        id: { dataType: DataType.Int32, isPartOfKey: true, isNullable: false }
      },
      navigationProperties: {
        userRights: { entityTypeName: "UserRight", isScalar: false, associationName: "BaseRights" }
      }
    });
    var orgT = metadataStore.addEntityType({
      shortName: "Organization",
      namespace: ns,
      baseTypeName: qt("Base"),
      dataProperties: {
        id: { dataType: DataType.String, isNullable: false }
      },
    });
    var urT = metadataStore.addEntityType({
      shortName: "UserRight",
      namespace: ns,
      dataProperties: {
        id: { dataType: DataType.Int32, isPartOfKey: true, isNullable: false },
        baseId: { dataType: DataType.Int32, isNullable: false }
      },
      navigationProperties: {
        base: { entityTypeName: "Base", associationName: "BaseRights", foreignKeyNames: [ "baseId"] }
      }
    });
  };


  function initMsForPersonMeal(metadataStore) {
    var Validator = breeze.Validator;
    var x = metadataStore.addEntityType({
      shortName: "Shift",
      namespace: testFns.sampleNamespace,
      isComplexType: true,
      dataProperties: {
        startDate: {
          dataType: DataType.DateTime,
          validators: Validator.fromJSON([
            { name: "date" },
            { name: "required" }
          ])
        },
        numHours: { dataType: DataType.Int32 }
      }
    });


    metadataStore.addEntityType({
      shortName: "Person",
      namespace: testFns.sampleNamespace,
      dataProperties: {
        personId: { dataType: DataType.Int32, isNullable: false, isPartOfKey: true },
        firstName: { dataType: DataType.String, isNullable: false },
        lastName: { dataType: DataType.String, isNullable: false },
        birthDate: { dataType: DataType.DateTime },
        shifts: { complexTypeName: "Shift:#" + testFns.sampleNamespace, isScalar: false }
      },
      navigationProperties: {
        meals: { entityTypeName: "Meal", isScalar: false, associationName: "personMeals" }
      }
    });

    metadataStore.addEntityType({
      shortName: "Meal",
      namespace: testFns.sampleNamespace,
      dataProperties: {
        mealId: { dataType: DataType.Int32, isNullable: false, isPartOfKey: true },
        personId: { dataType: DataType.Int32, isNullable: false },
        dateConsumed: { dataType: DataType.DateTime, isNullable: false }
      },
      navigationProperties: {
        person: { entityTypeName: "Person", isScalar: true, associationName: "personMeals", foreignKeyNames: ["personId"] },
        dishes: { entityTypeName: "Dish", isScalar: false, associationName: "mealDishes" }
      }
    });

    var et = new EntityType({
      shortName: "Dish",
      namespace: testFns.sampleNamespace,
      dataProperties: {
        dishId: { dataType: DataType.Int32, isNullable: false, isPartOfKey: true },
        foodName: { dataType: DataType.String, isNullable: false },
        servingSize: { dataType: DataType.Double, isNullable: false }
      },
      navigationProperties: {
        food: { entityTypeName: "Food", isScalar: true, associationName: "DishFood", foreignKeyNames: ["foodName"] }
      }
    });
    metadataStore.addEntityType(et);

    et = new EntityType({
      shortName: "Food",
      namespace: testFns.sampleNamespace,
      dataProperties: {
        foodName: { dataType: DataType.String, isNullable: false, isPartOfKey: true },
        calories: { dataType: DataType.Int32, isNullable: false }
      }
    });
    metadataStore.addEntityType(et);
  }

  function initMsForPersonMeal_long(metadataStore) {
    var et = new EntityType({
      shortName: "Person",
      namespace: testFns.sampleNamespace
    });
    et.addProperty(new DataProperty({
      name: "personId",
      dataType: DataType.Int32,
      isNullable: false,
      isPartOfKey: true
    }));
    et.addProperty(new DataProperty({
      name: "firstName",
      // dataType: DataType.String,
      isNullable: false
    }));
    et.addProperty(new DataProperty({
      name: "lastName",
      // dataType: DataType.String,
      isNullable: false
    }));
    et.addProperty(new DataProperty({
      name: "birthDate",
      dataType: DataType.DateTime
      // isNullable: true
    }));
    et.addProperty(new NavigationProperty({
      name: "meals",
      entityTypeName: "Meal",
      isScalar: false,
      associationName: "personMeals"
    }));
    metadataStore.addEntityType(et);

    et = new EntityType({
      shortName: "Meal",
      namespace: testFns.sampleNamespace
    });
    et.addProperty(new DataProperty({
      name: "mealId",
      dataType: DataType.Int32,
      isNullable: false,
      isPartOfKey: true
    }));
    et.addProperty(new DataProperty({
      name: "personId",
      dataType: DataType.Int32,
      isNullable: false
    }));
    et.addProperty(new DataProperty({
      name: "dateConsumed",
      dataType: DataType.DateTime,
      isNullable: false
    }));
    et.addProperty(new NavigationProperty({
      name: "person",
      entityTypeName: "Person",
      isScalar: true,
      associationName: "personMeals",
      foreignKeyNames: ["personId"]
    }));
    et.addProperty(new NavigationProperty({
      name: "dishes",
      entityTypeName: "Dish",
      isScalar: false,
      associationName: "mealDishes"
    }));
    metadataStore.addEntityType(et);

    et = new EntityType({
      shortName: "Dish",
      namespace: testFns.sampleNamespace
    });
    et.addProperty(new DataProperty({
      name: "dishId",
      dataType: DataType.Int32,
      isNullable: false,
      isPartOfKey: true
    }));
    et.addProperty(new DataProperty({
      name: "foodName",
      dataType: DataType.String,
      isNullable: false
    }));
    et.addProperty(new DataProperty({
      name: "servingSize",
      dataType: DataType.Double,
      isNullable: false
    }));
    et.addProperty(new NavigationProperty({
      name: "food",
      entityTypeName: "Food",
      isScalar: true,
      associationName: "DishFood",
      foreignKeyNames: ["foodName"]
    }));
    metadataStore.addEntityType(et);

    et = new EntityType({
      shortName: "Food",
      namespace: testFns.sampleNamespace
    });
    et.addProperty(new DataProperty({
      name: "foodName",
      dataType: DataType.String,
      isNullable: false,
      isPartOfKey: true
    }));
    et.addProperty(new DataProperty({
      name: "calories",
      dataType: DataType.Int32,
      isNullable: false
    }));
    metadataStore.addEntityType(et);
  }


})(breezeTestFns);