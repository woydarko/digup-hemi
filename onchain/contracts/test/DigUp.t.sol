// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {Test} from "forge-std/Test.sol";
import {ECDSA} from "@openzeppelin/contracts/utils/cryptography/ECDSA.sol";
import {Ownable} from "@openzeppelin/contracts/access/Ownable.sol";
import {DigUp} from "../src/DigUp.sol";

/// Minimal ERC-1271 smart wallet: valid when its owner key signed the hash.
contract MockWallet {
    address immutable owner;

    constructor(address owner_) {
        owner = owner_;
    }

    function isValidSignature(bytes32 hash, bytes memory signature) external view returns (bytes4) {
        return ECDSA.recover(hash, signature) == owner ? bytes4(0x1626ba7e) : bytes4(0xffffffff);
    }
}

contract DigUpTest is Test {
    DigUp game;
    address relayer = makeAddr("relayer");
    uint256 alicePk = 0xA11CE;
    address alice;

    function setUp() public {
        game = new DigUp(relayer);
        alice = vm.addr(alicePk);
    }

    function _digest(address player, uint32 score, bool escaped, uint256 nonce, uint256 deadline)
        internal
        view
        returns (bytes32)
    {
        bytes32 structHash = keccak256(abi.encode(game.RUN_TYPEHASH(), player, score, escaped, nonce, deadline));
        return keccak256(abi.encodePacked("\x19\x01", game.domainSeparator(), structHash));
    }

    function _sign(uint256 pk, uint32 score, bool escaped, uint256 nonce, uint256 deadline)
        internal
        view
        returns (bytes memory)
    {
        (uint8 v, bytes32 r, bytes32 s) = vm.sign(pk, _digest(vm.addr(pk), score, escaped, nonce, deadline));
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

    // Audit finding: a self-signed run must not land unless the relayer sends it
    function test_nonRelayer_reverts() public {
        uint256 deadline = block.timestamp + 600;
        bytes memory sig = _sign(alicePk, 1_000_000, true, 0, deadline);
        vm.prank(alice);
        vm.expectRevert(DigUp.NotRelayer.selector);
        game.submitRun(alice, 1_000_000, true, deadline, sig);
    }

    function test_replay_reverts() public {
        uint256 deadline = block.timestamp + 600;
        bytes memory sig = _sign(alicePk, 100, false, 0, deadline);
        vm.prank(relayer);
        game.submitRun(alice, 100, false, deadline, sig);
        vm.prank(relayer);
        vm.expectRevert(DigUp.BadSignature.selector);
        game.submitRun(alice, 100, false, deadline, sig);
    }

    function test_forgedScore_reverts() public {
        uint256 deadline = block.timestamp + 600;
        bytes memory sig = _sign(alicePk, 100, false, 0, deadline);
        vm.prank(relayer);
        vm.expectRevert(DigUp.BadSignature.selector);
        game.submitRun(alice, 999_999, false, deadline, sig);
    }

    function test_expired_reverts() public {
        uint256 deadline = block.timestamp + 600;
        bytes memory sig = _sign(alicePk, 100, false, 0, deadline);
        vm.warp(deadline + 1);
        vm.prank(relayer);
        vm.expectRevert(DigUp.Expired.selector);
        game.submitRun(alice, 100, false, deadline, sig);
    }

    // Audit lead: a signed run cannot be held for a long time
    function test_deadlineTooFar_reverts() public {
        uint256 deadline = block.timestamp + game.MAX_DEADLINE_WINDOW() + 1;
        bytes memory sig = _sign(alicePk, 100, false, 0, deadline);
        vm.prank(relayer);
        vm.expectRevert(DigUp.DeadlineTooFar.selector);
        game.submitRun(alice, 100, false, deadline, sig);
    }

    function test_scoreTooHigh_reverts() public {
        uint256 deadline = block.timestamp + 600;
        uint32 score = game.MAX_SCORE() + 1;
        bytes memory sig = _sign(alicePk, score, false, 0, deadline);
        vm.prank(relayer);
        vm.expectRevert(DigUp.ScoreTooHigh.selector);
        game.submitRun(alice, score, false, deadline, sig);
    }

    // Audit finding: escaped with score 0 minted a badge
    function test_escapedWithZeroScore_reverts() public {
        uint256 deadline = block.timestamp + 600;
        bytes memory sig = _sign(alicePk, 0, true, 0, deadline);
        vm.prank(relayer);
        vm.expectRevert(DigUp.InvalidRun.selector);
        game.submitRun(alice, 0, true, deadline, sig);
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

    // Audit lead: ERC-5192 lock signal, approvals rejected
    function test_badge_isLockedAndNotApprovable() public {
        _submit(alicePk, 300, true);
        assertTrue(game.locked(1));
        assertTrue(game.supportsInterface(0xb45a3c0e));
        assertTrue(game.supportsInterface(0x80ac58cd)); // ERC-721 still reported

        vm.prank(alice);
        vm.expectRevert(DigUp.Soulbound.selector);
        game.approve(relayer, 1);
        vm.prank(alice);
        vm.expectRevert(DigUp.Soulbound.selector);
        game.setApprovalForAll(relayer, true);
    }

    // Audit lead: smart-contract wallets can submit through ERC-1271
    function test_erc1271Wallet_canSubmit() public {
        MockWallet wallet = new MockWallet(alice);
        uint256 deadline = block.timestamp + 600;
        (uint8 v, bytes32 r, bytes32 s) = vm.sign(alicePk, _digest(address(wallet), 700, true, 0, deadline));
        vm.prank(relayer);
        game.submitRun(address(wallet), 700, true, deadline, abi.encodePacked(r, s, v));
        assertEq(game.bestScore(address(wallet)), 700);
        assertEq(game.balanceOf(address(wallet)), 1);
    }

    // Audit finding: the owner can remove a cheating wallet, which frees its slot
    function test_ban_removesFromTopAndBlocksRuns() public {
        for (uint256 i = 1; i <= 10; i++) {
            _submit(i, 1_000_000, false); // board full of maxed-out entries
        }
        game.ban(vm.addr(3));
        DigUp.Entry[] memory top = game.getTop();
        assertEq(top.length, 9);
        for (uint256 i; i < top.length; i++) assertTrue(top[i].player != vm.addr(3));
        assertEq(game.bestScore(vm.addr(3)), 0);

        _submit(alicePk, 5_000, false); // an honest player now gets in
        assertEq(game.getTop()[9].player, alice);

        uint256 deadline = block.timestamp + 600;
        bytes memory sig = _sign(3, 10, false, game.nonces(vm.addr(3)), deadline);
        vm.prank(relayer);
        vm.expectRevert(DigUp.Banned.selector);
        game.submitRun(vm.addr(3), 10, false, deadline, sig);
    }

    function test_ownerOnly_admin() public {
        vm.startPrank(alice);
        vm.expectRevert(abi.encodeWithSelector(Ownable.OwnableUnauthorizedAccount.selector, alice));
        game.ban(relayer);
        vm.expectRevert(abi.encodeWithSelector(Ownable.OwnableUnauthorizedAccount.selector, alice));
        game.setRelayer(alice);
        vm.stopPrank();

        game.setRelayer(alice);
        assertEq(game.relayer(), alice);
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
