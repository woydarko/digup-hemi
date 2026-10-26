// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {Script, console} from "forge-std/Script.sol";
import {DigUp} from "../src/DigUp.sol";

/// RELAYER=<relayer wallet address> forge script script/Deploy.s.sol --rpc-url hemi_testnet --account <keystore> --broadcast
/// The deployer becomes the owner (can ban wallets and change the relayer).
contract Deploy is Script {
    function run() external returns (DigUp game) {
        address relayer = vm.envAddress("RELAYER");
        vm.startBroadcast();
        game = new DigUp(relayer);
        vm.stopBroadcast();
        console.log("DigUp deployed at", address(game));
    }
}
