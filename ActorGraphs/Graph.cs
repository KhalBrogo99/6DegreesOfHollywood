using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMDbLib.Objects.Changes;
using TMDbLib.Objects.People;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using TMDbLib.Objects;
using TMDbLib.Client;
using TMDbLib.Utilities;
using TMDbLib.Objects.Movies;

namespace ActorGraphs
{
    class Graph
    {
        public Dictionary<int, List<Tuple<int, int>>> graph;
        private HashSet<int> visitedActors = new HashSet<int>();
        private int actorCap = 15;
        private int degree = 2;


        public Graph(Person actor1, Person actor2, TMDbClient client)
        {
            graph = new Dictionary<int, List<Tuple<int, int>>>();
            Populate(actor1, client);
            while (degree < 7)
            {
                var sources = new List<int>(graph.Keys);
                for (int i = 0; i < sources.Count; i++)
                {
                    var src = sources[i];
                    if (!visitedActors.Contains(src))
                    {
                        var newActor = client.GetPersonAsync(src, PersonMethods.MovieCredits).Result;
                        Populate(newActor, client);
                        if (graph.ContainsKey(actor2.Id))
                            return;
                    }
                }
                degree++;
            }
        }

        public void Populate(Person actor1, TMDbClient client)
        {
            List<MovieRole> movies = actor1.MovieCredits.Cast;

            //Initializing a new list of IDs that will hold the movie IDs
            List<int> MovieIDs = new List<int>();

            //Adding movie IDs to list
            //foreach is like an iterator through a container, very simple
            foreach (MovieRole movie in movies)
                MovieIDs.Add(movie.Id);

            //Initializing list of connected actors to actor1
            List<Cast> connectedActors = new List<Cast>();
            List<int> connectedMovies = new List<int>();

            //Marking starting actor visited
            visitedActors.Add(actor1.Id);

            HashSet<int> dupeActors = new HashSet<int>(visitedActors);

            //A list of all the cast in every movie Henry has been in
            //AddRange basically adds a list to a list (vector to a vector)

            //Initializing list for actor if it isn't already in the graph
            if (!graph.ContainsKey(actor1.Id))
                graph.Add(actor1.Id, new List<Tuple<int, int>>());

            for (int i = 0; i < MovieIDs.Count; i++)
            {
                var MovieID = MovieIDs[i];
                try
                {
                    var castMembers = client.GetMovieAsync(MovieID, MovieMethods.Credits).Result.Credits.Cast;
                    if (castMembers.Count > actorCap)
                        castMembers.RemoveRange(actorCap, castMembers.Count - actorCap);
                    List<int> connectedMovieIDs = Enumerable.Repeat(MovieID, castMembers.Count).ToList();
                    connectedActors.AddRange(castMembers);
                    connectedMovies.AddRange(connectedMovieIDs);
                }
                catch (Exception e) //Some movie IDs come back invalid because they seem to be deleted from the website/database
                {
                    continue;
                }
            }
            for (int i = 0; i < 200; i++)
            {
                try
                {
                    if (!dupeActors.Contains(connectedActors[i].Id))
                    {
                        if (!graph.ContainsKey(connectedActors[i].Id))
                            graph.Add(connectedActors[i].Id, new List<Tuple<int, int>>());
                        graph[actor1.Id].Add(new Tuple<int, int>(connectedActors[i].Id, connectedMovies[i]));
                        dupeActors.Add(connectedActors[i].Id);
                    }
                }
                catch (Exception e) //Out of bounds exceptions caused by capping connections
                {
                    break;
                }
                
            }
        }
        public bool DFS(TMDbClient client, Person actor1, Person actor2)
        {
            HashSet<int> visited = new HashSet<int>();
            Stack<Tuple<int, int, int>> s = new Stack<Tuple<int, int, int>>(); //Actor ID, actor ID of the actor they are connected to, and movie ID
            Tuple<int, int, int> src = new Tuple<int, int, int>(actor1.Id, 0, 0);
            //Tuple<int, int, int> dest = new Tuple<int, int, int>(actor2.Id, 0, 0);

            visited.Add(actor1.Id);
            s.Push(new Tuple<int,int,int>(actor1.Id,0,0));
            while (s.Count != 0)
            {
                var u = s.Peek();
                var neighbors = graph[u.Item1];
                foreach (var neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor.Item1))
                    {
                        visited.Add(neighbor.Item1);
                        s.Push(new Tuple<int, int, int>(neighbor.Item1, u.Item1, neighbor.Item2));

                    }
                    if (neighbor.Item1 == actor2.Id)
                    {
                        Unwind(s, client, actor1, actor2);
                        return true;
                    }
                }
                s.Pop();
            }
            return false;
        }

        public bool BFS(TMDbClient client, Person actor1, Person actor2)
        {
            HashSet<int> visited = new HashSet<int>();
            Queue<Tuple<int, int, int>> q = new Queue<Tuple<int, int, int>>(); //Actor ID, actor ID of the actor they are connected to, and movie ID
            Tuple<int, int, int> src = new Tuple<int, int, int>(actor1.Id, 0, 0);
            //Tuple<int, int, int> dest = new Tuple<int, int, int>(actor2.Id, 0, 0);

            visited.Add(actor1.Id);
            q.Enqueue(new Tuple<int, int, int>(actor1.Id, 0, 0));
            while (q.Count != 0)
            {
                var u = q.Peek();
                var neighbors = graph[u.Item1];
                foreach (var neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor.Item1))
                    {
                        visited.Add(neighbor.Item1);
                        q.Enqueue(new Tuple<int, int, int>(neighbor.Item1, u.Item1, neighbor.Item2));
                    }
                    if (neighbor.Item1 == actor2.Id)
                    {
                        Unwind(q, client, actor1, actor2);
                        return true;
                    }
                }
                q.Dequeue();
            }
            return false;
        }

        public void Unwind(Stack<Tuple<int, int, int>> s, TMDbClient client, Person actor1, Person actor2)
        {
            while (s.Count != 0)
            {
                string top = client.GetPersonAsync(s.Peek().Item1).Result.Name;
                int connection = s.Peek().Item2;
                int movie = s.Peek().Item3;
                while (s.Peek().Item1 != connection)
                    s.Pop();
                string next = client.GetPersonAsync(s.Peek().Item1).Result.Name;
                string movieName = client.GetMovieAsync(movie).Result.Title;
                Console.WriteLine(top + " is in " + movieName + " with " + next);
                if (next == actor1.Name)
                    return;
            }
        }

        public void Unwind(Queue<Tuple<int, int, int>> q, TMDbClient client, Person actor1, Person actor2)
        {
            int nextID = actor2.Id;
            while (q.Count != 0)
            {
                Queue<Tuple<int, int, int>> tempQ = new Queue<Tuple<int, int, int>>(q);
                while (tempQ.Peek().Item1 != nextID)
                    tempQ.Dequeue();
                string top = client.GetPersonAsync(tempQ.Peek().Item1).Result.Name;
                string next = client.GetPersonAsync(tempQ.Peek().Item2).Result.Name;
                string movieName = client.GetMovieAsync(tempQ.Peek().Item3).Result.Title;
                Console.WriteLine(top + " is in " + movieName + " with " + next);
                if (next == actor1.Name)
                    return;
                else
                    nextID = tempQ.Peek().Item2;
            }
        }
    }
    class CastEqualityComparer : IEqualityComparer<Cast>
    {
        //Comparer class needs the following two functions to work correctly
        public bool Equals(Cast c1, Cast c2)
        {
            if (c1.Id == c2.Id)
                return true;
            else
                return false;
        }
        public int GetHashCode(Cast c1)
        {
            int ID = c1.Id;
            return ID.GetHashCode();
        }
    }
}
