namespace MongoDB.Entities.Utilities
{
    public class ProfilerLog
    {
        public string op { get; set; }
        public string ns { get; set; }
        public Command command { get; set; }
        public int keysExamined { get; set; }
        public int docsExamined { get; set; }
        public bool cursorExhausted { get; set; }
        public int numYield { get; set; }
        public int nreturned { get; set; }
        public Locks locks { get; set; }
        public ReadConcern readConcern { get; set; }
        public WriteConcern writeConcern { get; set; }
        public int responseLength { get; set; }
        public string protocol { get; set; }
        public int millis { get; set; }
        public string planSummary { get; set; }
        public string ts { get; set; }
        public string client { get; set; }
        public string appName { get; set; }
        public object[] allUsers { get; set; }
        public string user { get; set; }

        public class Command
        {
            public string aggregate { get; set; }
            public Pipeline[] pipeline { get; set; }
            public Cursor cursor { get; set; }
            public string db { get; set; }
        }

        public class Cursor
        {
            public int batchSize { get; set; }
        }

        public class Pipeline
        {
            public Sample sample { get; set; }
        }

        public class Sample
        {
            public int size { get; set; }
        }

        public class Locks
        {
            public Global Global { get; set; }
            public Mutex Mutex { get; set; }
        }

        public class Global
        {
            public Acquirecount acquireCount { get; set; }
        }

        public class Acquirecount
        {
            public long r { get; set; }
        }

        public class Mutex
        {
            public Acquirecount acquireCount { get; set; }
        }

        public class ReadConcern
        {
            public string level { get; set; }
            public string provenance { get; set; }
        }

        public class WriteConcern
        {
            public string w { get; set; }
            public int wtimeout { get; set; }
            public string provenance { get; set; }
        }

    }
}
