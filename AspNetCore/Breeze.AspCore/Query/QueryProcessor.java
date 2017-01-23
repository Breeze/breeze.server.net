package com.breeze.query;

import com.breeze.metadata.IEntityType;
import com.breeze.metadata.Metadata;

/**
 * Abstract base class for all query processing logic. 
 * @author IdeaBlade
 *
 */
public abstract class QueryProcessor {
    
    protected Metadata _metadata;
    protected QueryProcessor(Metadata metadata) {
        _metadata = metadata;
    }
    
    public QueryResult executeQuery(String resourceName, String json) {
        EntityQuery entityQuery = new EntityQuery(json);
        return executeQuery(resourceName, entityQuery);
    }

    public QueryResult executeQuery(Class clazz, String json) {
        EntityQuery entityQuery = new EntityQuery(json);
        return executeQuery(clazz, entityQuery);
    }

    public QueryResult executeQuery(EntityQuery entityQuery) {
        String resourceName = entityQuery.getResourceName();
        if (resourceName == null)
            return null;
        return executeQuery(resourceName, entityQuery);
    }
    
    public QueryResult executeQuery(String resourceName, EntityQuery entityQuery) {
        IEntityType entityType = _metadata.getEntityTypeForResourceName(resourceName);
        return executeQuery(entityType, entityQuery);
    }

    public QueryResult executeQuery(Class clazz, EntityQuery entityQuery) {
        IEntityType entityType = _metadata.getEntityTypeForClass(clazz);
        return executeQuery(entityType, entityQuery);
    }

    protected abstract QueryResult executeQuery(IEntityType entityType, EntityQuery entityQuery);
}
