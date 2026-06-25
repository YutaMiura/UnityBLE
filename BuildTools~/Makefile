# UnityBLE native build targets.
#
# Produces the three native artifacts consumed by the UnityBLE UPM package
# and drops them directly under Runtime/Plugins/<platform>/ (overwriting any
# previous build).
#
# Usage (run from anywhere — use -C to point make at this file):
#   make -C BuildTools~ ios
#   make -C BuildTools~ macos
#   make -C BuildTools~ android
#   make -C BuildTools~ all
#   make -C BuildTools~ clean
#   make -C BuildTools~ help
#
# Requirements:
#   - macOS host with Xcode + command line tools (for ios / macos)
#   - JDK 11+ and a configured Android SDK (for android). Either set
#     ANDROID_HOME or populate externalProjects~/Android/UnityBLE/local.properties
#     with `sdk.dir=...`.

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------

# Directory that contains this Makefile.
MAKEFILE_DIR := $(abspath $(dir $(lastword $(MAKEFILE_LIST))))
# UnityBLE UPM package root (parent of BuildTools~).
ROOT         := $(abspath $(MAKEFILE_DIR)/..)

XCODE_PROJ      := $(ROOT)/externalProjects~/Xcode/UnityBlePlugin/UnityBlePlugin.xcodeproj
ANDROID_PROJ    := $(ROOT)/externalProjects~/Android/UnityBLE

# Intermediate build artifacts live here so they stay out of Unity and out of
# the published package. .gitignore'd alongside this Makefile.
BUILD_DIR       := $(MAKEFILE_DIR)/build
IOS_BUILD_DIR   := $(BUILD_DIR)/ios
MACOS_BUILD_DIR := $(BUILD_DIR)/macos

PLUGINS_IOS     := $(ROOT)/Runtime/Plugins/iOS
PLUGINS_MACOS   := $(ROOT)/Runtime/Plugins/macOS
PLUGINS_ANDROID := $(ROOT)/Runtime/Plugins/Android

CONFIG          ?= Release

# Names of the artifacts produced. Must match what the C# loader expects.
IOS_FRAMEWORK   := UnityBlePlugin.framework
MACOS_BUNDLE    := UnityBlePluginBundle.bundle
ANDROID_AAR     := UnityBLE.aar

# ---------------------------------------------------------------------------
# Targets
# ---------------------------------------------------------------------------

.PHONY: help all ios macos android clean

.DEFAULT_GOAL := help

help:
	@echo "UnityBLE native build targets:"
	@echo "  make ios      - Build $(IOS_FRAMEWORK) into Runtime/Plugins/iOS/"
	@echo "  make macos    - Build $(MACOS_BUNDLE) into Runtime/Plugins/macOS/"
	@echo "  make android  - Build $(ANDROID_AAR) into Runtime/Plugins/Android/"
	@echo "  make all      - Build all three"
	@echo "  make clean    - Remove intermediate build artifacts"
	@echo ""
	@echo "Variables:"
	@echo "  CONFIG=Release|Debug  (default: Release)"

all: ios macos android

# ---------------------------------------------------------------------------
# iOS framework
# ---------------------------------------------------------------------------

ios:
	@echo ">>> Building iOS framework ($(CONFIG))"
	@mkdir -p "$(IOS_BUILD_DIR)" "$(PLUGINS_IOS)"
	xcodebuild \
	  -project "$(XCODE_PROJ)" \
	  -scheme UnityBlePlugin \
	  -sdk iphoneos \
	  -configuration $(CONFIG) \
	  -derivedDataPath "$(IOS_BUILD_DIR)" \
	  ONLY_ACTIVE_ARCH=NO \
	  BUILD_LIBRARY_FOR_DISTRIBUTION=YES \
	  SKIP_INSTALL=NO \
	  build
	@echo ">>> Installing $(IOS_FRAMEWORK) -> $(PLUGINS_IOS)/"
	rm -rf "$(PLUGINS_IOS)/$(IOS_FRAMEWORK)"
	cp -R "$(IOS_BUILD_DIR)/Build/Products/$(CONFIG)-iphoneos/$(IOS_FRAMEWORK)" "$(PLUGINS_IOS)/"
	@echo ">>> iOS done."

# ---------------------------------------------------------------------------
# macOS bundle
# ---------------------------------------------------------------------------

macos:
	@echo ">>> Building macOS bundle ($(CONFIG))"
	@mkdir -p "$(MACOS_BUILD_DIR)" "$(PLUGINS_MACOS)"
	xcodebuild \
	  -project "$(XCODE_PROJ)" \
	  -scheme UnityBlePluginBundle \
	  -sdk macosx \
	  -configuration $(CONFIG) \
	  -derivedDataPath "$(MACOS_BUILD_DIR)" \
	  build
	@echo ">>> Installing $(MACOS_BUNDLE) -> $(PLUGINS_MACOS)/"
	rm -rf "$(PLUGINS_MACOS)/$(MACOS_BUNDLE)"
	cp -R "$(MACOS_BUILD_DIR)/Build/Products/$(CONFIG)/$(MACOS_BUNDLE)" "$(PLUGINS_MACOS)/"
	@echo ">>> macOS done."

# ---------------------------------------------------------------------------
# Android AAR
# ---------------------------------------------------------------------------

# Gradle's release output filename. With the current android.library plugin
# config (namespace jp.yuta.miura.unityble, module name "app"), the artifact
# is app-release.aar.
ANDROID_GRADLE_OUTPUT := $(ANDROID_PROJ)/app/build/outputs/aar/app-release.aar
ANDROID_GRADLE_TASK   := assembleRelease

ifeq ($(CONFIG),Debug)
ANDROID_GRADLE_OUTPUT := $(ANDROID_PROJ)/app/build/outputs/aar/app-debug.aar
ANDROID_GRADLE_TASK   := assembleDebug
endif

# AGP 8.8.0 requires JDK 17+. The shell's default JAVA_HOME may still point
# at JDK 11 (Microsoft JDK is the common case on these dev machines), so
# auto-detect a usable JDK for the Android build only. Priority order:
#   1. ANDROID_JAVA_HOME (explicit override)
#   2. brew openjdk@17 (canonical install)
#   3. Android Studio bundled JBR (very common on dev machines; AS ships JDK 21)
#   4. /usr/libexec/java_home -v 17 (last resort)
#
# Note: /usr/libexec/java_home silently falls back to the default JDK when
# the requested -v is not found, so we cannot rely on it as the first probe.
ANDROID_JAVA_HOME ?= $(shell \
  if [ -d "/opt/homebrew/opt/openjdk@17/libexec/openjdk.jdk/Contents/Home" ]; then \
    echo "/opt/homebrew/opt/openjdk@17/libexec/openjdk.jdk/Contents/Home"; \
  elif [ -d "$$HOME/Applications/Android Studio.app/Contents/jbr/Contents/Home" ]; then \
    echo "$$HOME/Applications/Android Studio.app/Contents/jbr/Contents/Home"; \
  elif [ -d "/Applications/Android Studio.app/Contents/jbr/Contents/Home" ]; then \
    echo "/Applications/Android Studio.app/Contents/jbr/Contents/Home"; \
  elif candidate=$$(/usr/libexec/java_home -v 17 2>/dev/null) && \
       [ -x "$$candidate/bin/java" ] && \
       "$$candidate/bin/java" -version 2>&1 | grep -qE 'version "(1[7-9]|[2-9][0-9])\.'; then \
    echo "$$candidate"; \
  else echo ""; fi)

android:
	@if [ -z '$(ANDROID_JAVA_HOME)' ]; then \
	  echo "ERROR: JDK 17+ not found."; \
	  echo "  Install one of:"; \
	  echo "    - brew install openjdk@17"; \
	  echo "    - Android Studio (its bundled JBR is auto-detected)"; \
	  echo "  Or set ANDROID_JAVA_HOME=/path/to/jdk explicitly."; \
	  exit 1; \
	fi
	@echo ">>> Building Android AAR ($(CONFIG))"
	@echo ">>> Using JAVA_HOME=$(ANDROID_JAVA_HOME)"
	@mkdir -p "$(PLUGINS_ANDROID)"
	cd "$(ANDROID_PROJ)" && JAVA_HOME='$(ANDROID_JAVA_HOME)' ./gradlew :app:$(ANDROID_GRADLE_TASK)
	@echo ">>> Installing $(ANDROID_AAR) -> $(PLUGINS_ANDROID)/"
	rm -f "$(PLUGINS_ANDROID)/$(ANDROID_AAR)"
	cp "$(ANDROID_GRADLE_OUTPUT)" "$(PLUGINS_ANDROID)/$(ANDROID_AAR)"
	@echo ">>> Android done."

# ---------------------------------------------------------------------------
# Clean
# ---------------------------------------------------------------------------

clean:
	@echo ">>> Removing intermediate build artifacts"
	rm -rf "$(BUILD_DIR)"
	@if [ -n '$(ANDROID_JAVA_HOME)' ]; then \
	  cd "$(ANDROID_PROJ)" && JAVA_HOME='$(ANDROID_JAVA_HOME)' ./gradlew :app:clean || true; \
	else \
	  echo "(skipping gradle clean: JDK 17+ not detected)"; \
	fi
	@echo ">>> Clean done. (installed Runtime/Plugins artifacts left in place)"
