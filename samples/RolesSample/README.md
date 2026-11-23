# Roles Sample Overview

This sample extends the [Task Org sample](../TaskOrgSample), adding role switching:

* Choosing the role this client works in - S2, S3, S4, FSO or ENGINEER
* Following role switches, made by this client or by STP

Only the aspects particular to this sample are described here.
Details shared by all samples are described in the [main samples page](../README.md).

## STP roles

Each client works in a role. The role decides which COA new symbols and tasks go into (each role works on
its own current COA, see the [COA sample](../CoaSample)) and is recorded with the changes the client makes,
for example in a task's history (see the [Tasking sample](../TaskingSample)).

The contract's roles are the `StpRole` constants: `S2` (intelligence), `S3` (operations), `S4` (logistics),
`FSO` (fires) and `ENGINEER`. `StpRole.ENG` is an alias whose value is `ENGINEER`; earlier samples sent
`"ENG"`, which is not one of the contract's values. `StpRole.All` lists the five, and fills the Role box
([Form1.cs](./Form1.cs), `Connect()`):

```csharp
comboBoxRole.Items.AddRange(StpRole.All.ToArray<object>());
```

## Switching roles

Choosing a role in the Role box sends it to STP:

```csharp
await _stpRecognizer.SetRoleAsync(role);
```

`SetRoleAsync` also has an overload taking the contract's optional `createIfNone` (whether to create a
default COA for the role when it has none). The contract notes that the engine ignores it over this API, so
the sample does not send it.

STP reports every role switch with `OnRoleSwitched`, subscribed before connecting. The sample updates the
Role box without triggering another `SetRoleAsync`:

```csharp
private void StpRecognizer_OnRoleSwitched(string role)
{
    comboBoxRole.SelectedIndexChanged -= ComboBoxRole_SelectedIndexChanged;
    comboBoxRole.Text = role;
    comboBoxRole.SelectedIndexChanged += ComboBoxRole_SelectedIndexChanged;
}
```

A role switch can also bring in a different COA and with it a different task org; `OnTaskOrgSwitched`
(described in the [Task Org sample](../TaskOrgSample)) reports that.

## Running the sample

Build and run it as described in the [main samples page](../README.md#running-the-samples). Then:

1. Choose `S3` in the Role box and place a few symbols
2. Choose `ENGINEER`: the log shows `ROLE SWITCHED: ENGINEER`
3. Load the friendly task org with `Friend` and see `TO SWITCHED` in the log
