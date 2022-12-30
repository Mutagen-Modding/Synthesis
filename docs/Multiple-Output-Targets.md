<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [Synthesis Groups](#synthesis-groups)
  - [Adding a Group](#adding-a-group)
  - [Renaming a Group](#renaming-a-group)
  - [Adding Patchers to Group](#adding-patchers-to-group)
  - [Moving Patchers between Groups](#moving-patchers-between-groups)
- [Running With Multiple Groups](#running-with-multiple-groups)
  - [Run Whole Pipeline](#run-whole-pipeline)
  - [Keep Groups In Sync with Load Order](#keep-groups-in-sync-with-load-order)
- [Running a Single Group](#running-a-single-group)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

By default, Synthesis comes with one large group, named Synthesis.  Putting patchers inside and running them results in all of their content being added to one single mod named `Synthesis.esp`.

# Synthesis Groups
Synthesis can also be set up to export different patcher results into different mods.  Synthesis groups is a feature that helps organize what patchers will go into what mods.

Each group will export its contained patchers into a mod with the name of the group.
A group named `MyPatch` will be exported to a mod called `MyPatch.esp`

## Adding a Group
Clicking the new group button will add a fresh group to your pipeline

![](https://i.imgur.com/99v7YqM.png)

## Renaming a Group
After creating a new Group, you will want to give it a name before it is usable.  This can only be done by focusing the group and renaming it in the right side panel
![](https://i.imgur.com/cWTEkXz.gif)

## Adding Patchers to Group
With the group selected, you can then add patchers to the new group as normal.  Just make sure to click on the group you want to add the patcher to before adding the patcher.

## Moving Patchers between Groups
You can also drag drop patchers between groups, and reorder them if the patchers you're choosing care about order.

# Running With Multiple Groups
## Run Whole Pipeline
You can run the whole pipeline by clicking the run button at the bottom left of the UI.  This will run all of your groups in order one after another.

## Keep Groups In Sync with Load Order
One important suggestion to keep in mind is that the order groups are run in Synthesis should generally match the order that their output mods appear in the load order.  If you swap the order in one, make sure you sync and reorder the other.

The reason this matters is that each group sees the the other groups as just another mod on the list.  If the other group's output is on the load order and before it, it will read it in like any other mod.  But if they are out of order, then the groups that run after might not see the first groups as mods to consider (as they will come after them in the load order, and thus be ignored).

# Running a Single Group
You can also run a single group in isolation, rather than generating all groups at once.  This is sometimes useful, but generally is less safe than running the whole pipeline.  