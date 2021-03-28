using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Channels;
using BenchmarkDotNet.Attributes;

namespace Benchmarks.Benchmarks
{
    public class ReactiveBenchmark
    {
        private Subject<TestClass> _s;
        public event Action<TestClass> MyTestEvent; 

        public ReactiveBenchmark()
        {
            // channels for event producers -> metric producers
            // 
            _s = new Subject<TestClass>();
            _s.AsObservable().Subscribe(new Subscriber());
            var i = 0;
            //MyTestEvent += (e) => { i++; };
            _eventObs = Observable.FromEvent<TestClass>(h => MyTestEvent += h, r => {});
            _eventObs.Subscribe(new Subscriber());
        }

        public class Subscriber : IObserver<TestClass>
        {
            private int _i = 0;
            public void OnCompleted()
            {
                
            }

            public void OnError(Exception error)
            {
                
            }

            public void OnNext(TestClass value)
            {
                _i++;
            }
        }

        [Benchmark]
        public void TestPublishConsume()
        {
            _s.Publish(new TestClass());
        }
        
        private TestClass _fixed = new TestClass();
        private IObservable<TestClass> _eventObs;

        [Benchmark]
        public void TestPublishConsumeReuse()
        {
            _s.Publish(_fixed);
        }
        
        [Benchmark]
        public void TestEvent()
        {
            MyTestEvent.Invoke(new TestClass());
        }
        
        [Benchmark]
        public void TestEventReuse()
        {
            MyTestEvent.Invoke(_fixed);
        }
        
        [Benchmark]
        public void TestEventObservable()
        {
            MyTestEvent.Invoke(_fixed);
        }
        
        

        public class TestClass
        {
            public int SampleValue1 { get; set; }
            public string SampleValue2 { get; set; }
        }
    }
    
    public sealed class SubjectNoAlloc<T> : SubjectBase<T>
    {
        #region Fields

        private SubjectDisposable[] _observers;
        private Exception? _exception;
        private static readonly SubjectDisposable[] Terminated = new SubjectDisposable[0];
        private static readonly SubjectDisposable[] Disposed = new SubjectDisposable[0];

        #endregion

        #region Constructors
        //
        // /// <summary>
        // /// Creates a subject.
        // /// </summary>
        // public Subject() => _observers = Array.Empty<SubjectDisposable>();

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether the subject has observers subscribed to it.
        /// </summary>
        public override bool HasObservers => Volatile.Read(ref _observers).Length != 0;

        /// <summary>
        /// Indicates whether the subject has been disposed.
        /// </summary>
        public override bool IsDisposed => Volatile.Read(ref _observers) == Disposed;

        #endregion

        #region Methods

        #region IObserver<T> implementation

        private static void ThrowDisposed() => throw new ObjectDisposedException(string.Empty);

        /// <summary>
        /// Notifies all subscribed observers about the end of the sequence.
        /// </summary>
        public override void OnCompleted()
        {
            for (; ; )
            {
                var observers = Volatile.Read(ref _observers);

                if (observers == Disposed)
                {
                    _exception = null;
                    ThrowDisposed();
                    break;
                }

                if (observers == Terminated)
                {
                    break;
                }

                if (Interlocked.CompareExchange(ref _observers, Terminated, observers) == observers)
                {
                    foreach (var observer in observers)
                    {
                        observer.Observer?.OnCompleted();
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Notifies all subscribed observers about the specified exception.
        /// </summary>
        /// <param name="error">The exception to send to all currently subscribed observers.</param>
        /// <exception cref="ArgumentNullException"><paramref name="error"/> is null.</exception>
        public override void OnError(Exception error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            for (; ; )
            {
                var observers = Volatile.Read(ref _observers);

                if (observers == Disposed)
                {
                    _exception = null;
                    ThrowDisposed();
                    break;
                }

                if (observers == Terminated)
                {
                    break;
                }

                _exception = error;

                if (Interlocked.CompareExchange(ref _observers, Terminated, observers) == observers)
                {
                    foreach (var observer in observers)
                    {
                        observer.Observer?.OnError(error);
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Notifies all subscribed observers about the arrival of the specified element in the sequence.
        /// </summary>
        /// <param name="value">The value to send to all currently subscribed observers.</param>
        public override void OnNext(T value)
        {
            var observers = Volatile.Read(ref _observers);

            if (observers == Disposed)
            {
                _exception = null;
                ThrowDisposed();
                return;
            }

            foreach (var observer in observers)
            {
                observer.Observer?.OnNext(value);
            }
        }

        #endregion

        #region IObservable<T> implementation

        /// <summary>
        /// Subscribes an observer to the subject.
        /// </summary>
        /// <param name="observer">Observer to subscribe to the subject.</param>
        /// <returns>Disposable object that can be used to unsubscribe the observer from the subject.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="observer"/> is null.</exception>
        public override IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            var disposable = default(SubjectDisposable);
            for (; ; )
            {
                var observers = Volatile.Read(ref _observers);

                if (observers == Disposed)
                {
                    _exception = null;
                    ThrowDisposed();

                    break;
                }

                if (observers == Terminated)
                {
                    var ex = _exception;

                    if (ex != null)
                    {
                        observer.OnError(ex);
                    }
                    else
                    {
                        observer.OnCompleted();
                    }

                    break;
                }

                disposable ??= new SubjectDisposable(this, observer);

                var n = observers.Length;
                var b = new SubjectDisposable[n + 1];

                Array.Copy(observers, 0, b, 0, n);

                b[n] = disposable;

                if (Interlocked.CompareExchange(ref _observers, b, observers) == observers)
                {
                    return disposable;
                }
            }

            return Disposable.Empty;
        }

        private void Unsubscribe(SubjectDisposable observer)
        {
            for (; ; )
            {
                var a = Volatile.Read(ref _observers);
                var n = a.Length;

                if (n == 0)
                {
                    break;
                }

                var j = Array.IndexOf(a, observer);

                if (j < 0)
                {
                    break;
                }

                SubjectDisposable[] b;

                if (n == 1)
                {
                    b = Array.Empty<SubjectDisposable>();
                }
                else
                {
                    b = new SubjectDisposable[n - 1];

                    Array.Copy(a, 0, b, 0, j);
                    Array.Copy(a, j + 1, b, j, n - j - 1);
                }

                if (Interlocked.CompareExchange(ref _observers, b, a) == a)
                {
                    break;
                }
            }
        }

        private sealed class SubjectDisposable : IDisposable
        {
            private SubjectNoAlloc<T> _subject;
            private volatile IObserver<T>? _observer;

            public SubjectDisposable(SubjectNoAlloc<T> subject, IObserver<T> observer)
            {
                _subject = subject;
                _observer = observer;
            }

            public IObserver<T>? Observer => _observer;

            public void Dispose()
            {
                var observer = Interlocked.Exchange(ref _observer, null);
                if (observer == null)
                {
                    return;
                }

                _subject.Unsubscribe(this);
                _subject = null!;
            }
        }

        #endregion

        #region IDisposable implementation

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="Subject{T}"/> class and unsubscribes all observers.
        /// </summary>
        public override void Dispose()
        {
            Interlocked.Exchange(ref _observers, Disposed);
            _exception = null;
            
        }

        #endregion

        #endregion
    }
}