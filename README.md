# 🛠️ Custom Unity Tools

A modular suite of lightweight editor utilities, workflow enhancements, and development tools crafted to make everyday Unity authoring faster, cleaner, and smoother.

---

## 💡 Overview

Game development often involves repetitive inspector adjustments, manual scene setup, and boilerplate tooling. **Custom Unity Tools** eliminates that friction by providing a plug-and-play collection of battle-tested editor enhancements and runtime helpers.

Whether you're automating asset processing, optimizing asynchronous logic with **UniTask**, or managing serialized game data with **Newtonsoft.Json**, this package integrates seamlessly into your existing projects via the Unity Package Manager (UPM).

### Key Highlights
- **⚡ Accelerated Workflow**: Reduce clicks and repetitive tasks with tailored editor menus, inspector enhancements, and shortcuts.
- **🔄 Async-First Architecture**: Built on top of **UniTask** for high-performance, allocation-free asynchronous routines.
- **📦 Clean Serialization**: Integrated with **Newtonsoft.Json for Unity** for robust configuration and state management.
- **🧩 Zero-Friction Integration**: Non-invasive, modular design separated cleanly into Editor and Runtime assemblies (`.asmdef`).

## Installation
### Via Unity Package Manager (Git URL)
1. In Unity, go to **Window** > **Package Manager**.
2. Click the **+** (plus) icon in the top-left corner.
3. Select **Add package from git URL...**.
4. Paste the repository URL:
   ```text
   https://github.com/Sahil785/CustomUnityTools.git