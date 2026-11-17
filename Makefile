POWERSHELL ?= powershell
-include local.mk

.PHONY: all build

all: build

build:
	$(POWERSHELL) -NoProfile -ExecutionPolicy Bypass -File Build.ps1 $(if $(GAME_DIR),-GameDir "$(GAME_DIR)")
