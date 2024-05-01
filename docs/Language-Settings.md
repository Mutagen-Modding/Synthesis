# Language Settings
Synthesis comes with a few profile settings to help users that are targeting a language besides English.  The typical settings you should pick depend on your situation and the patterns that your language translation mods have used.

## Overview
### Localize
This setting controls whether to use `.STRINGS` file system when exporting your synthesis patches.  This will allow for a synthesis patch to export with multiple languages at once and with their associated preferred encoding, as each language is stored in their own files.

### Language Dropdown
This determines what language a mod which is not localized (does not have .strings files) should be considered to have.  This is important for different reasons depending on if you have the above Localized setting turned on:
- If you have the above Localization setting turned on, when patchers have records from mods that were originally not localized, their string will be placed into the language strings file specified in this dropdown.
- If you have the above Localization setting turned off, then when patchers have records from mods that were originally localized, it will take the language data from the strings file associated with the language from this dropdown and put that into the outgoing non-localized patch files.

### Use UTF8 for Embedded Strings
By default, if Localization is turned off for a mod and .strings files are not used, the embedded strings are encoded with a non-UTF8 encoding.  But, some language modding setups have mods with non-localized strings that are stored as UTF8 (often Japanese, Chinese, etc).  This setting controls whether Synthesis should use UTF8 when reading embedded strings or not.   For default English users, this is usually good to keep off.

## Typical Recommended Settings
### For English Users
Default settings is good:

- `Localization` -> Off
- `Language Dropdown` -> English
- `Use UTF8 for Embedded Strings` -> Off

### For Non-English Users
A current best practice is not yet ironed out (please feel free to swing by the discord and offer your experience and suggestions!). 

The desired settings depend on the ecosystem your language community has adopted.  For example, Japanese/Chinese translation mods have seemingly opted to not use `.strings` files, and instead embed their strings into non-localized files that store with the UTF8 encoding.  Thus, for those setups, you would want to turn off `Localization`, and turn on `Use UTF8`.  

But that might not be consistent with other language ecosystems or your specific setup.  The best recommendation is to read the available settings and try to navigate what combination will work for your setup.  If you find what works for you, please give some feedback so that this documentation can be better filled out!
