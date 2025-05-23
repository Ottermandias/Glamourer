using Glamourer.Api.Enums;
using Glamourer.Events;
using Glamourer.State;
using OtterGui.Services;
using Penumbra.GameData.Interop;

namespace Glamourer.Designs.History;

public class EditorHistory : IDisposable, IService
{
    public const int MaxUndo = 16;

    private sealed class Queue : IReadOnlyList<ITransaction>
    {
        private DateTime _lastAdd = DateTime.UtcNow;

        private readonly ITransaction[] _data = new ITransaction[MaxUndo];
        public           int            Offset { get; private set; }
        public           int            Count  { get; private set; }

        public void Add(ITransaction transaction)
        {
            if (!TryMerge(transaction))
            {
                if (Count == MaxUndo)
                {
                    _data[Offset] = transaction;
                    Offset        = (Offset + 1) % MaxUndo;
                }
                else
                {
                    if (Offset > 0)
                    {
                        _data[(Count + Offset) % MaxUndo] = transaction;
                        ++Count;
                    }
                    else
                    {
                        _data[Count] = transaction;
                        ++Count;
                    }
                }
            }

            _lastAdd = DateTime.UtcNow;
        }

        private bool TryMerge(ITransaction newTransaction)
        {
            if (Count == 0)
                return false;

            var time = DateTime.UtcNow;
            if (time - _lastAdd > TimeSpan.FromMilliseconds(250))
                return false;

            var lastIdx = (Offset + Count - 1) % MaxUndo;
            if (newTransaction.Merge(_data[lastIdx]) is not { } transaction)
                return false;

            _data[lastIdx] = transaction;
            return true;
        }

        public ITransaction? RemoveLast()
        {
            if (Count == 0)
                return null;

            --Count;
            var idx = (Offset + Count) % MaxUndo;
            return _data[idx];
        }

        public IEnumerator<ITransaction> GetEnumerator()
        {
            var end = Offset + (Offset + Count) % MaxUndo;
            for (var i = Offset; i < end; ++i)
                yield return _data[i];

            end = Count - end;
            for (var i = 0; i < end; ++i)
                yield return _data[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public ITransaction this[int index]
            => index < 0 || index >= Count
                ? throw new IndexOutOfRangeException()
                : _data[(Offset + index) % MaxUndo];
    }

    private readonly DesignEditor  _designEditor;
    private readonly StateEditor   _stateEditor;
    private readonly DesignChanged _designChanged;
    private readonly StateChanged  _stateChanged;

    private readonly Dictionary<ActorState, Queue> _stateEntries  = [];
    private readonly Dictionary<Design, Queue>     _designEntries = [];

    private bool _undoMode;

    public EditorHistory(DesignManager designEditor, StateManager stateEditor, DesignChanged designChanged, StateChanged stateChanged)
    {
        _designEditor  = designEditor;
        _stateEditor   = stateEditor;
        _designChanged = designChanged;
        _stateChanged  = stateChanged;

        _designChanged.Subscribe(OnDesignChanged, DesignChanged.Priority.EditorHistory);
        _stateChanged.Subscribe(OnStateChanged, StateChanged.Priority.EditorHistory);
    }

    public void Dispose()
    {
        _designChanged.Unsubscribe(OnDesignChanged);
        _stateChanged.Unsubscribe(OnStateChanged);
    }

    public bool CanUndo(ActorState state)
        => _stateEntries.TryGetValue(state, out var list) && list.Count > 0;

    public bool CanUndo(Design design)
        => _designEntries.TryGetValue(design, out var list) && list.Count > 0;

    public bool Undo(ActorState state)
    {
        if (!_stateEntries.TryGetValue(state, out var list) || list.Count == 0)
            return false;

        _undoMode = true;
        list.RemoveLast()!.Revert(_stateEditor, state);
        _undoMode = false;
        return true;
    }

    public bool Undo(Design design)
    {
        if (!_designEntries.TryGetValue(design, out var list) || list.Count == 0)
            return false;

        _undoMode = true;
        list.RemoveLast()!.Revert(_designEditor, design);
        _undoMode = false;
        return true;
    }


    private void AddStateTransaction(ActorState state, ITransaction transaction)
    {
        if (!_stateEntries.TryGetValue(state, out var list))
        {
            list = [];
            _stateEntries.Add(state, list);
        }

        list.Add(transaction);
    }

    private void AddDesignTransaction(Design design, ITransaction transaction)
    {
        if (!_designEntries.TryGetValue(design, out var list))
        {
            list = [];
            _designEntries.Add(design, list);
        }

        list.Add(transaction);
    }


    private void OnStateChanged(StateChangeType type, StateSource source, ActorState state, ActorData actors, ITransaction? data)
    {
        if (_undoMode || source is not StateSource.Manual)
            return;

        if (data is not null)
            AddStateTransaction(state, data);
    }

    private void OnDesignChanged(DesignChanged.Type type, Design design, ITransaction? data)
    {
        if (_undoMode)
            return;

        if (data is not null)
            AddDesignTransaction(design, data);
    }
}
