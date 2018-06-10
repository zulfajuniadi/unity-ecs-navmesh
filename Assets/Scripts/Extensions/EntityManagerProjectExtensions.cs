using Unity.Collections;
using Unity.Entities;

public static class EntityManagerProjectExtensions
{
    public static Entity GetEntityByIndex (this EntityManager entityManager, int index)
    {
        var entities = entityManager.GetAllEntities (Allocator.Temp);
        var entity = Entity.Null;
        if (index < entities.Length && index > 0)
        {
            entity = entities[index];
        }

        entities.Dispose ();
        return entity;
    }
}