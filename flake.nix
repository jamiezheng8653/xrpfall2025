{
  inputs = {
    nixpkgs.url = "nixpkgs";
    systems.url = "github:nix-systems/default";
    flake-utils = {
      url = "github:numtide/flake-utils";
      inputs.systems.follows = "systems";
    };
  };

  outputs = { self, nixpkgs, flake-utils, ... }:
  flake-utils.lib.eachDefaultSystem ( system:
    let
      pkgs = import nixpkgs { inherit system; config.allowUnfree = true; config.microsoftVisualStudioLicenseAccepted = true;};
    in {
      devShell = pkgs.mkShell { # nix shell
      packages = with pkgs; [
        godotPackages_4_4.godot-mono
        ];
        buildInputs = with pkgs; [
        ];
      };
    }
  );
}
