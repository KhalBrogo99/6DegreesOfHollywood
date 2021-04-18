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
    //This graph is implemented as an adjacency list
    class Graph
    {
        //Dictonary is like a c++ map, list is like a c++ vector, and Tuples are n-size grouped elements
        //Each tuple contains the following: <ID of connected actor, Movie ID of connected movie>
        public Dictionary<int, List<Tuple<int, int>>> graph;

        //Set to mark actors as visited as the graph populates
        private HashSet<int> visitedActors = new HashSet<int>();

        //The cap for how many actors to take from each movie (chosen because most movies don't have many famous actors
        private int actorCap = 15;

        //A connection cap for how many vertices can be connected to any one vertex
        //Set to 200 for testing
        private int connectionCap = 200;

        //Starting degree of separation used to count how far from the initial actor the graph has populated
        private int degree = 2;




        public Graph(Person actor1, Person actor2, TMDbClient client)
        {
            /* This graph constructor uses a depth first search style function *
             * to populate the graph. For testing purposes the graph will stop *
             * populating once the second actor is found. For an extended test *
             * the graph was populated up to when the actor was found AND once *
             * the degree was finished.                                        */

            //Initializing graph
            graph = new Dictionary<int, List<Tuple<int, int>>>();

            //Populating the graph for the first/initial actor
            Populate(actor1, client);

            //Populating up to 6 degrees of separation
            while (degree < 7)
            {
                //Preserving the current keys in a list prevents an exception when adding more keys to the graph
                //NOTE: var is like the c++ auto keyword
                var sources = new List<int>(graph.Keys);

                //Going through all actors in this degree of separation
                for (int i = 0; i < sources.Count; i++)
                {
                    //Source is the current actor's ID in the database
                    var src = sources[i];

                    //If the actor hasn't been visited
                    if (!visitedActors.Contains(src))
                    {
                        //Getting the actual person of the actor ID
                        var newActor = client.GetPersonAsync(src, PersonMethods.MovieCredits).Result;

                        //Populating graph with the current actor's connected actors
                        Populate(newActor, client);

                        //Stopping the population once the second actor is found.
                        //If an extended test is needed, replace "return" with
                        //"degree = 7". A similar result will be found.
                        if (graph.ContainsKey(actor2.Id))
                            return;
                    }
                }
                degree++;
            }
        }

        //Helper function that does the actual populating
        public void Populate(Person actor1, TMDbClient client)
        {
            //Getting the movies that the actor has been in
            List<MovieRole> movies = actor1.MovieCredits.Cast;

            //Initializing a new list of IDs that will hold the movie IDs
            List<int> MovieIDs = new List<int>();

            //Adding movie IDs to list
            foreach (MovieRole movie in movies)
                MovieIDs.Add(movie.Id);

            //Initializing list of connected actors and their movies to actor1
            List<Cast> connectedActors = new List<Cast>();
            List<int> connectedMovies = new List<int>();

            //Marking starting actor visited
            visitedActors.Add(actor1.Id);

            //This set takes care of when an actor has worked with an actor multiple times
            //Originally initialized to be the same as visited actors because if an actor
            //has been visited it is either not connected to the current actor or it 
            //is already in the graph as a connection
            HashSet<int> dupeActors = new HashSet<int>(visitedActors);

            //Initializing list for actor if it isn't already in the graph
            if (!graph.ContainsKey(actor1.Id))
                graph.Add(actor1.Id, new List<Tuple<int, int>>());

            //For how many movies there are
            for (int i = 0; i < MovieIDs.Count; i++)
            {
                //Current movie ID
                var MovieID = MovieIDs[i];

                //A try catch block is needed because some movie IDs provide dead links/IDs that get nullReferenceExceptions
                try
                {
                    //Getting a list of all the cast members in the movie
                    var castMembers = client.GetMovieAsync(MovieID, MovieMethods.Credits).Result.Credits.Cast;

                    //Condensing it down to the top 15 actors, if there are more than 15
                    if (castMembers.Count > actorCap)
                        castMembers.RemoveRange(actorCap, castMembers.Count - actorCap);

                    //Creating a list of the connected movie (the current movie repeated for the amount of actors)
                    List<int> connectedMovieIDs = Enumerable.Repeat(MovieID, castMembers.Count).ToList();

                    //Adding actors and movies to their lists
                    connectedActors.AddRange(castMembers);
                    connectedMovies.AddRange(connectedMovieIDs);
                }
                //If a movie is a dead link, just move on
                catch (Exception e)
                {
                    continue;
                }
            }
            for (int i = 0; i < connectionCap; i++)
            {
                //Try catch to handle out of bounds exceptions caused by capping connections
                //Some actors have not worked with 200 actors
                try
                {
                    //If not already added to graph
                    if (!dupeActors.Contains(connectedActors[i].Id))
                    {
                        //If the graph does not have this actor, add it as a separate vertext
                        if (!graph.ContainsKey(connectedActors[i].Id))
                            graph.Add(connectedActors[i].Id, new List<Tuple<int, int>>());

                        //Add current actor as a connection to this actor, along with the movie connection
                        graph[actor1.Id].Add(new Tuple<int, int>(connectedActors[i].Id, connectedMovies[i]));

                        //Add this actor to set of duplicates
                        dupeActors.Add(connectedActors[i].Id);
                    }
                }
                //If the list is out of actors to access, break
                catch (Exception e)
                {
                    break;
                }
            }
        }

        //Depth first search on the graph
        public bool DFS(TMDbClient client, Person actor1, Person actor2)
        {
            //Set of visited actors (using actor IDs)
            HashSet<int> visited = new HashSet<int>();

            //Tuple: <Actor ID, actor ID of the actor they are connected to, and movie ID of connection>
            Stack<Tuple<int, int, int>> s = new Stack<Tuple<int, int, int>>(); 

            //Mark first actor visited
            visited.Add(actor1.Id);

            //Push to stack
            s.Push(new Tuple<int,int,int>(actor1.Id,0,0));

            //Standard DFS algorithm, with added case for when it finds the second actor
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
                    
                    //Once it finds the actor, call this Unwind function
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

        //Same as above but for breadth first search, standard algorithm with an added case for when it finds the second actor
        public bool BFS(TMDbClient client, Person actor1, Person actor2)
        {
            HashSet<int> visited = new HashSet<int>();
            Queue<Tuple<int, int, int>> q = new Queue<Tuple<int, int, int>>(); //Actor ID, actor ID of the actor they are connected to, and movie ID
            
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

        //Unwind overloaded for stack
        public void Unwind(Stack<Tuple<int, int, int>> s, TMDbClient client, Person actor1, Person actor2)
        {
            //As per DFS, the top of the stack of is the second actor

            //While the stack is not empty
            while (s.Count != 0)
            {
                //TOP is in MOVIE with NEXT -> top = next -> TOP is in MOVIE with NEXT

                //Getting the actor's name
                string top = client.GetPersonAsync(s.Peek().Item1).Result.Name;

                //Connected actor
                int connection = s.Peek().Item2;

                //Connected movie
                int movie = s.Peek().Item3;

                //Finding connected actor
                while (s.Peek().Item1 != connection)
                    s.Pop();

                //Next is the connected actor
                string next = client.GetPersonAsync(s.Peek().Item1).Result.Name;

                //Getting movie name
                string movieName = client.GetMovieAsync(movie).Result.Title;

                //Printing
                Console.WriteLine(top + " is in " + movieName + " with " + next);

                //If the connected actor is the original actor, the code is finished
                if (next == actor1.Name)
                    return;
            }
        }

        //Unwind overloaded for Queue
        public void Unwind(Queue<Tuple<int, int, int>> q, TMDbClient client, Person actor1, Person actor2)
        {
            //With BFS, the next in the queue is not the correct actor, so it must be found

            //NEXT is the second actor
            int nextID = actor2.Id;
            while (q.Count != 0)
            {

                //Copying queue
                Queue<Tuple<int, int, int>> tempQ = new Queue<Tuple<int, int, int>>(q);

                //Find the current actor (actor2 in initial case)
                while (tempQ.Peek().Item1 != nextID)
                    tempQ.Dequeue();

                //top is the current actor, next is the next connected actor, movieName is the name of the connected movie
                string top = client.GetPersonAsync(tempQ.Peek().Item1).Result.Name;
                string next = client.GetPersonAsync(tempQ.Peek().Item2).Result.Name;
                string movieName = client.GetMovieAsync(tempQ.Peek().Item3).Result.Title;

                //Printing out connections
                Console.WriteLine(top + " is in " + movieName + " with " + next);

                //If the next actor is the original, the code has finished
                //otherwise the new current actor is the current actor's next
                if (next == actor1.Name)
                    return;
                else
                    nextID = tempQ.Peek().Item2;
            }
        }
    }



    /* This is leftover from when we were making sets of Cast objects,
     * but we moved to just using the IDs */

    /*
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
    }*/
}
