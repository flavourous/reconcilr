using System.Collections.Specialized;
using System.Collections;
using Reconcilr;

var original = new AppState(new Catalog(new User("Ada", 36), new[] { "Reader" }, new[] { new User("Lin", 20) }));
var stateOwner = new StateOwner(original);
var shadow = stateOwner.State;
Assert(shadow.Catalog.Profile.Name == "Ada", "Strings should be exposed as scalar properties rather than reconciled character collections.");
var rolesList = (IList)shadow.Catalog.Roles;
Assert(rolesList.IsReadOnly, "Generated reconciled collections should be exposed as read-only IList instances.");
var changed = new List<string>();
var collectionActions = new List<NotifyCollectionChangedAction>();
var memberChanges = new List<string>();
var memberCollectionActions = new List<NotifyCollectionChangedAction>();

shadow.Catalog.Profile.PropertyChanged += (_, eventArgs) => changed.Add(eventArgs.PropertyName!);
shadow.Catalog.Roles.CollectionChanged += (_, eventArgs) => collectionActions.Add(eventArgs.Action);
shadow.Catalog.Members[0].PropertyChanged += (_, eventArgs) => memberChanges.Add(eventArgs.PropertyName!);
shadow.Catalog.Members.CollectionChanged += (_, eventArgs) => memberCollectionActions.Add(eventArgs.Action);

stateOwner.SetState(new AppState(new Catalog(new User("Ada", 37), new[] { "Writer", "Admin" }, new[] { new User("Lin", 21) })));

Assert(changed.SequenceEqual(new[] { "Age" }), "Only the changed scalar should notify.");
Assert(collectionActions.SequenceEqual(new[] { NotifyCollectionChangedAction.Replace, NotifyCollectionChangedAction.Add }), "Changed and appended scalar items should notify replace and add.");
Assert(memberChanges.SequenceEqual(new[] { "Age" }), "A changed complex item should notify through its shadow.");
Assert(memberCollectionActions.Count == 0, "A partially changed item should not raise a collection replacement.");

var propertyRoot = new PropertyRoot(new GenericState<int>(3, new[] { 3 }));
var propertyShadow = propertyRoot.MyState;
propertyRoot.SetMyState(new GenericState<int>(4, new[] { 4 }));
Assert(propertyShadow.Value == 4, "An attributed constructed generic property should generate a fully bound shadow.");

var genericOwner = new GenericOwner<GenericState<int>>(new GenericState<int>(5, new[] { 5 }));
var genericOwnerShadow = genericOwner.Model;
var genericOwnerChanges = new List<string?>();
genericOwnerShadow.PropertyChanged += (_, eventArgs) => genericOwnerChanges.Add(eventArgs.PropertyName);
genericOwner.SetModel(new GenericState<int>(6, new[] { 6 }));
Assert(genericOwnerChanges.SequenceEqual(new string?[] { null }), "A generic type parameter shadow should notify consumers to refresh after replacement.");
Console.WriteLine("Reconcilr smoke test passed.");

static void Assert(bool condition, string message)
{
	if (!condition)
	{
		throw new InvalidOperationException(message);
	}
}

public sealed record AppState(Catalog Catalog);

public sealed record Catalog(User Profile, string[] Roles, User[] Members);

public sealed record User(string Name, int Age);

public sealed partial class StateOwner
{
	[Reconcile]
	private AppState _state;

	public StateOwner(AppState state) => _state = state;
}

public sealed record GenericState<T>(T Value, T[] Values);

public sealed partial class PropertyRoot
{
	[Reconcile]
	private GenericState<int> _myState;

	public PropertyRoot(GenericState<int> myState) => _myState = myState;
}

public sealed partial class GenericOwner<T>
	where T : class
{
	[Reconcile]
	private T _model;

	public GenericOwner(T model) => _model = model;
}
