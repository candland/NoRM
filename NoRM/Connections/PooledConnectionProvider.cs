namespace NoRM
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    //todo: review (thrown together quickly)
    internal class PooledConnectionProvider : ConnectionProvider
    {
        private const int MAXIMUM_POOL_SIZE = 5; //todo: make configurable via connection string
        private const int TIMEOUT = 15000; //todo: make configurable via connection string

        private readonly ConnectionStringBuilder _builder;
        private readonly Queue<IConnection> _idlePool;
        private readonly Semaphore _tracker; //todo: semaphore a little heavy?
        
        public override ConnectionStringBuilder ConnectionString
        {
            get { return _builder; }
        }
                
        public PooledConnectionProvider(ConnectionStringBuilder builder)
        {
            _builder = builder;
            _idlePool = new Queue<IConnection>(MAXIMUM_POOL_SIZE);
            _tracker = new Semaphore(0, MAXIMUM_POOL_SIZE);
            for(var i = 0; i < MAXIMUM_POOL_SIZE; ++i)
            {
                EnqueueIdle(CreateNewConnection());
            }            
        }

        public override IConnection Open(string options)
        {
            if (!_tracker.WaitOne(TIMEOUT))
            {
                throw new TimeoutException();
            }
            var connection = _idlePool.Dequeue();
            if (!string.IsNullOrEmpty(options))
            {
                connection.LoadOptions(options);
            }
            return connection;
        }

        public override void Close(IConnection connection)
        {
            EnqueueIdle(connection);
        }
        private void EnqueueIdle(IConnection connection)
        {
            connection.ResetOptions();
            _idlePool.Enqueue(connection);
            _tracker.Release();
        }
    }
}