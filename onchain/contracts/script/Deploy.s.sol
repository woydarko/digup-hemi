// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {Script, console} from "forge-std/Script.sol";
import {DigUp} from "../src/DigUp.sol";

/// forge script script/Deploy.s.sol --rpc-url hemi_testnet --account <keystore> --broadcast
contract Deploy is Script {
    function run() external returns (DigUp game) {
        vm.startBroadcast();
        game = new DigUp();
        vm.stopBroadcast();
        console.log("DigUp deployed at", address(game));
    }
}
