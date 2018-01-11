using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Breeze.Core {

  /**
   * Represents a single expand expand clause that will be part of an EntityQuery. An expand 
   * clause represents the path to other entity types via a navigation path from the current EntityType
   * for a given query. 
   * @author IdeaBlade
   *
   */
  public class ExpandClause {
    private List<String> _propertyPaths;


    public static ExpandClause From(IEnumerable propertyPaths) {
      return (propertyPaths == null) ? null : new ExpandClause(propertyPaths.Cast<String>());
    }

    public ExpandClause(IEnumerable<String> propertyPaths) {
      _propertyPaths = propertyPaths.ToList();
    }


    public IEnumerable<String> PropertyPaths {
      get { return _propertyPaths.AsReadOnly(); }
    }

  }

}