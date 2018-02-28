public static class Globals
    {
        public static Camera Camera { get; set; }

        public static Vector3 GetMouseClickPosition(InputEventMouseButton mouseEvent)
        {
            var from = Camera.ProjectRayOrigin(mouseEvent.GlobalPosition);
            var to = from + Camera.ProjectRayNormal(mouseEvent.GlobalPosition) * 100;
            return Raycast(from, to);
        }

        public static Vector3 Raycast(Vector3 from, Vector3 to)
        {
            var collision = PhysicsServer.SpaceGetDirectState(Camera.GetWorld().GetSpace()).IntersectRay(from, to).GetEnumerator();
            collision.MoveNext();
            return (Vector3)collision.Current.Value;
        }

        public static IEnumerable<object> GetCollision(Vector3 from, Vector3 to)
        {
            var enumerator = PhysicsServer.SpaceGetDirectState(Camera.GetWorld().GetSpace()).IntersectRay(from, to).GetEnumerator();
            while (enumerator.MoveNext())
                yield return enumerator.Current.Value;
        }

        public static IEnumerable<Vector3> RaycastMany(Vector3[] from, Vector3[] to)
        {
            var directSpace = PhysicsServer.SpaceGetDirectState(Camera.GetWorld().GetSpace());
            for (int i = 0; i < from.Length; i++)
            {
                var collision = directSpace.IntersectRay(from[i], to[i]).GetEnumerator();
                collision.MoveNext();
                if (collision.Current.Value != null)
                    yield return (Vector3)collision.Current.Value;
            }
        }

        public static IEnumerable<Vector3> RaycastManyFromSingle(Vector3 from, Vector3[] to)
        {
            var directSpace = PhysicsServer.SpaceGetDirectState(Camera.GetWorld().GetSpace());
            for (int i = 0; i < to.Length; i++)
            {
                var collision = directSpace.IntersectRay(from, to[i]).GetEnumerator();
                collision.MoveNext();
                if (collision.Current.Value != null)
                    yield return (Vector3)collision.Current.Value;
                else
                    yield return Constants.Zero;
            }
        }

        public static Vector3 RandomCubicPoint(float min, float max)
        {
            return new Vector3(UniRand.NextFloat(min, max), UniRand.NextFloat(min, max), UniRand.NextFloat(min, max));
        }

        public static Vector3 RandomPoint(float minx, float maxx, float miny, float maxy, float minz, float maxz)
        {
            return new Vector3(UniRand.NextFloat(minx, maxx), UniRand.NextFloat(miny, maxy), UniRand.NextFloat(minz, maxz));
        }

        public static Vector3 GroundPositionFrom(Vector3 here)
        {
            var collision = PhysicsServer
                .SpaceGetDirectState(Camera.GetWorld().GetSpace())
                .IntersectRay(here, Constants.Bottom)
                .GetEnumerator();
            if (collision.MoveNext())
                return (Vector3)collision.Current.Value;
            return Constants.Zero;
        }
    }
