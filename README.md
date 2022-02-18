![PepperDash Logo](/images/logo_pdt_no_tagline_600.png)
## License
Provided under MIT license
# PepperDash Essentials DENTON-COR-TAP Plugin (c) 2022

This repo contains a plugin for use with [PepperDash Essentials](https://github.com/PepperDash/Essentials). This plugin enables Essentials to communicate with and control the DENTON-COR-TAP via HTTP.

### Essentials Device Configuration
```json
{
  "key": "lights-1",
  "uid": 1,
  "name": "Lights",
  "group": "plugin",
  "type": "dentoncortap",
  "properties": {
    "url": "192.168.1.100",
    "fixtureName": "Fixture-Name",
    "scenes": [
      {"id": 1, "name": "On"},
      {"id": 2, "name": "Off"}
    ]
  }         
},
```
## Device Bridging

### Essentials Device Bridge Configuration

```json
 {
        "key": "lighting-bridge-1",
        "uid": 2,
        "name": "Lighting Bridge 1",
        "group": "api",
        "type": "eiscApiAdvanced",
        "properties": {
          "control": {
            "tcpSshProperties": {
              "address": "127.0.0.2",
              "port": 0
            },
            "ipid": "ac",
            "method": "ipidTcp"
          },
          "devices": [
            {
              "deviceKey": "lights-1",
              "joinStart": 1
            }
          ]
        }
      }
```
### Essentials Bridge Join Map

The join map below documents the commands implemented in this plugin.

### Digitals

| Input                         | I/O | Output                    |
| ----------------------------- | --- | ------------------------- |
|                               | 1   | Device Online Fb          |
|                               | +   |                           |
| Recall Scene 1                | 11  |                           |
| Recall Scene 2                | 12  |                           |
| Recall Scene 3                | 13  |                           |
| Recall Scene 4                | 14  |                           |
| Recall Scene 5                | 15  |                           |
| Recall Scene 6                | 16  |                           |

### Analogs

| Input                         | I/O | Output                    |
| ----------------------------- | --- | ------------------------- |
| Fixture Level Set             | 1   | Fixture Level Feedback    |

### Serials

| Input | I/O | Output                      |
| ----- | --- | --------------------------- |
|       | 1   | Device Name                 |
|       | 2   | Fixture Name                |
|       | +   |                             |
|       | 11  | Scene Name (1-10)           |

## Github Actions

This repo contains two Github Action workflows that will build this project automatically. Modify the SOLUTION_PATH and SOLUTION_FILE environment variables as needed. Any branches named `feature/*`, `release/*`, `hotfix/*` or `development` will automatically be built with the action and create a release in the repository with a version number based on the latest release on the master branch. If there are no releases yet, the version number will be 0.0.1. The version number will be modified based on what branch triggered the build:

- `feature` branch builds will be tagged with an `alpha` descriptor, with the Action run appended: `0.0.1-alpha-1`
- `development` branch builds will be tagged with a `beta` descriptor, with the Action run appended: `0.0.1-beta-2`
- `release` branches will be tagged with an `rc` descriptor, with the Action run appended: `0.0.1-rc-3`
- `hotfix` branch builds will be tagged with a `hotfix` descriptor, with the Action run appended: `0.0.1-hotfix-4`

Builds on the `Main` branch will ONLY be triggered by manually creating a release using the web interface in the repository. They will be versioned with the tag that is created when the release is created. The tags MUST take the form `major.minor.revision` to be compatible with the build process. A tag like `v0.1.0-alpha` is NOT compatabile and may result in the build process failing.

If you have any questions about the action, contact Andrew Welker or Neil Dorin.