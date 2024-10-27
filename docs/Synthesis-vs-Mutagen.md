# Synthesis vs Mutagen

Often newcomers are first introduced to Synthesis.  They install a patcher or want to make their own.
But what is the term Mutagen, and why is it mentioned often alongside Synthesis?

Both of these terms are used in conjunction with each other often, and so they can get confusing.

## Mutagen
[Mutagen](https://github.com/Mutagen-Modding/Mutagen) is the library that allows for reading and manipulating Bethesda plugin files.  

Sorts of things that Mutagen is in charge of:

- Reading what an Npc's Health is
- Making 10 new Weapons with various stats
- Finding/reading a texture asset from a BSA
- Resolving and finding the Race that an Npc is
- Other jobs that require interacting with mods and analyzing/manipulating their data

## Synthesis
[Synthesis](https://github.com/Mutagen-Modding/Synthesis) is a system that is built on top of Mutagen to provide an easy patcher pipeline ecosystem.  

Things that Synthesis is in charge of:

- A UI for users to control what patchers they want to install, in what groups, in what order.
- Running dozens of patcher code snippets (patchers) from many mod authors easily in one place with one click
- Creating an easy bootstrap project template to start coding your own patcher that hooks into the system

## Summary
Synthesis is the patcher pipeline ecosystem that helps create an accessible environment for developers to create and for users to run Mutagen code snippets.   You do not need to find/install Mutagen yourself as an installation step for Synthesis, it will be pulled in automatically.
