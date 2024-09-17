# QuestsAreInSkyrimPatcher

Synthesis patcher for [Quests are in Skyrim](https://www.nexusmods.com/skyrimspecialedition/mods/18416). The patcher forwards the formlist condition that QAIS adds to radiant quests to ensure subsequent quests are only assigned to locations within Skyrim and not mod-added worlds. This patcher is intended to allow QAIS to work alongside other quest mods such as [At Your Own Pace](https://www.nexusmods.com/skyrimspecialedition/mods/52704) and [Skyrim Realistic Conquering](https://www.nexusmods.com/skyrimspecialedition/mods/26396), which also edit radiant quests, without having to make complex combination patches manually for all affected quests.

## Usage
- Supports either version of the QAIS plugin (USSEP and non-USSEP)
- Add to your Synthesis pipeline using the patcher browser
- If you have multiple Synthesis groups, run this patcher in the same group as other patchers that also modify quests to ensure changes are merged properly.
- The patcher will log which quests and aliases it forwarded the condition to. These can be viewed in the Synthesis log files or in the UI itself.

**NOTE**: There are some quests where QAIS adds an `OR` condition that effectively ignores the Formlist check which will result in quests outside of Skyrim. Pending a mod update, this patcher does *not* forward that extra condition, just the formlist one. See the screenshots below for a better visual explanation.

## Reporting Bugs/Issues
Please include the following to help me help you:
- Synthesis log file(s)
- `Plugins.txt`
- Specific record(s) that are problematic
  - xEdit screenshots not required, but appreciated


## Examples:
**Note** that xEdit tends to show conflicts with lists like conditions, even if they contain the same items. What's important is that the condition is present in the winning record as marked in the screenshots. In a future update I might add logic to try and place the condition in the same spot in the list, but there's no gaurantee that will remove the visual conflicts in xedit.

![Example](/examples/example-and.jpg)

![Example where the formlist is added but not the OR'd condition](/examples/example-or.jpg)