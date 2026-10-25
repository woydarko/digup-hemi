// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {Test} from "forge-std/Test.sol";
import {DigUp} from "../src/DigUp.sol";

contract DigUpTest is Test {
    DigUp game;
    address relayer = makeAddr("relayer");
    uint256 alicePk = 0xA11CE;
    address alice;

    function setUp() public {
        game = new DigUp();
        alice = vm.addr(alicePk);
    }

    function _sign(uint256 pk, uint32 score, bool escaped, uint256 nonce, uint256 deadline)
        internal
        view
        returns (bytes memory)
    {
        bytes32 structHash = keccak256(abi.encode(game.RUN_TYPEHASH(), vm.addr(pk), score, escaped, nonce, deadline));
        bytes32 digest = keccak256(abi.encodePacked("\x19\x01", game.domainSeparator(), structHash));
        (uint8 v, bytes32 r, bytes32 s) = vm.sign(pk, digest);
        return abi.encodePacked(r, s, v);
    }

    function _submit(uint256 pk, uint32 score, bool escaped) internal {
        address player = vm.addr(pk);
        uint256 deadline = block.timestamp + 600;
        bytes memory sig = _sign(pk, score, escaped, game.nonces(player), deadline);
        vm.prank(relayer);
        game.submitRun(player, score, escaped, deadline, sig);
    }

    function test_submit_recordsBestScore() public {
        _submit(alicePk, 420, false);
        assertEq(game.bestScore(alice), 420);
        assertEq(game.totalRuns(), 1);
        assertEq(game.nonces(alice), 1);
        DigUp.Entry[] memory top = game.getTop();
        assertEq(top.length, 1);
        assertEq(top[0].player, alice);
        assertEq(top[0].score, 420);
        assertEq(game.balanceOf(alice), 0);
    }

    function test_lowerScore_keepsBest() public {
        _submit(alicePk, 500, false);
        _submit(alicePk, 100, false);
        assertEq(game.bestScore(alice), 500);
        assertEq(game.getTop()[0].score, 500);
        assertEq(game.totalRuns(), 2);
    }

    function test_replay_reverts() public {
        uint256 deadline = block.timestamp + 600;
        bytes memory sig = _sign(alicePk, 100, false, 0, deadline);
        game.submitRun(alice, 100, false, deadline, sig);
        vm.expectRevert(DigUp.BadSignature.selector);
        game.submitRun(alice, 100, false, deadline, sig);
    }

    function test_forgedScore_reverts() public {
        uint256 deadline = block.timestamp + 600;
        bytes memory sig = _sign(alicePk, 100, false, 0, deadline);
        vm.expectRevert(DigUp.BadSignature.selector);
        game.submitRun(alice, 999_999, false, deadline, sig);
    }

    function test_expired_reverts() public {
        uint256 deadline = block.timestamp + 600;
        bytes memory sig = _sign(alicePk, 100, false, 0, deadline);
        vm.warp(deadline + 1);
        vm.expectRevert(DigUp.Expired.selector);
        game.submitRun(alice, 100, false, deadline, sig);
    }

    function test_scoreTooHigh_reverts() public {
        uint256 deadline = block.timestamp + 600;
        uint32 score = game.MAX_SCORE() + 1;
        bytes memory sig = _sign(alicePk, score, false, 0, deadline);
        vm.expectRevert(DigUp.ScoreTooHigh.selector);
        game.submitRun(alice, score, false, deadline, sig);
    }

    function test_escape_mintsSoulboundBadgeOnce() public {
        _submit(alicePk, 300, true);
        _submit(alicePk, 900, true);
        assertEq(game.balanceOf(alice), 1);
        assertEq(game.badgeOf(alice), 1);
        assertEq(game.totalEscapes(), 2);
        assertGt(bytes(game.tokenURI(1)).length, 0);

        vm.prank(alice);
        vm.expectRevert(DigUp.Soulbound.selector);
        game.transferFrom(alice, relayer, 1);
    }

    function test_top_sortedCappedOnePerPlayer() public {
        for (uint256 i = 1; i <= 12; i++) {
            _submit(i, uint32((i * 37) % 100 + 1), false);
        }
        _submit(5, 1000, false); // improve an existing player
        DigUp.Entry[] memory top = game.getTop();
        assertEq(top.length, game.TOP_SIZE());
        assertEq(top[0].player, vm.addr(5));
        for (uint256 i = 1; i < top.length; i++) {
            assertGe(top[i - 1].score, top[i].score);
            for (uint256 j; j < i; j++) assertTrue(top[i].player != top[j].player);
        }
    }
}
