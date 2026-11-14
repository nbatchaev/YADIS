# YADIS.OhioT1DM.Parser

CLI tool for converting **OhioT1DM** dataset XML files into binary **MessagePack** (`.mpk`) archives using [`YADIS.OhioT1DM.Models`](https://github.com/nbatchaev/YADIS/tree/main/YADIS.OhioT1DM.Models).

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/nbatchaev/YADIS/blob/main/LICENSE)

## Overview

The parser reads patient XML files from the 2018 (18-tag) and 2020 (19-tag) OhioT1DM cohorts, validates their structure against `OhioT1DmPatient`, fixes malformed tag ordering in `596-ws` records, converts glucose values from $\text{mg/dL}$ to $\text{mmol/L}$, and writes `.mpk` files split into `training` and `testing` folders.

## Usage

```bash
dotnet run --project YADIS.OhioT1DM.Parser
```

## License

Licensed under the [MIT License](https://github.com/nbatchaev/YADIS/blob/main/LICENSE).