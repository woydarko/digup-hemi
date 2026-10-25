// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {ERC721} from "@openzeppelin/contracts/token/ERC721/ERC721.sol";
import {EIP712} from "@openzeppelin/contracts/utils/cryptography/EIP712.sol";
import {ECDSA} from "@openzeppelin/contracts/utils/cryptography/ECDSA.sol";
import {Base64} from "@openzeppelin/contracts/utils/Base64.sol";
import {Strings} from "@openzeppelin/contracts/utils/Strings.sol";

/// @title Dig Up on Hemi
/// @notice Onchain runs for the Dig Up game:
///  - gasless score submission: the player signs an EIP-712 `Run`, anyone (our relayer) sends the tx
///  - an onchain top-10 leaderboard
///  - a soulbound "Escaped" badge for players who reach the finish flag
contract DigUp is ERC721, EIP712 {
    struct Entry {
        address player;
        uint32 score;
        uint64 timestamp;
    }

    bytes32 public constant RUN_TYPEHASH =
        keccak256("Run(address player,uint32 score,bool escaped,uint256 nonce,uint256 deadline)");

    uint256 public constant TOP_SIZE = 10;
    uint32 public constant MAX_SCORE = 1_000_000;

    mapping(address => uint256) public nonces;
    mapping(address => uint32) public bestScore;
    mapping(address => uint256) public badgeOf; // 0 = no badge

    uint256 public totalRuns;
    uint256 public totalEscapes;

    uint256 private _nextTokenId = 1;
    Entry[] private _top;

    event RunSubmitted(address indexed player, uint32 score, bool escaped);
    event BadgeMinted(address indexed player, uint256 indexed tokenId);

    error Expired();
    error BadSignature();
    error ScoreTooHigh();
    error Soulbound();

    constructor() ERC721("Dig Up: Escaped", "DIGUP") EIP712("DigUp", "1") {}

    /// @notice Record a run signed by `player`. Callable by anyone, so the player never pays gas.
    function submitRun(address player, uint32 score, bool escaped, uint256 deadline, bytes calldata signature)
        external
    {
        if (block.timestamp > deadline) revert Expired();
        if (score > MAX_SCORE) revert ScoreTooHigh();

        uint256 nonce = nonces[player]++;
        bytes32 structHash = keccak256(abi.encode(RUN_TYPEHASH, player, score, escaped, nonce, deadline));
        if (ECDSA.recover(_hashTypedDataV4(structHash), signature) != player) revert BadSignature();

        totalRuns++;

        if (score > bestScore[player]) {
            bestScore[player] = score;
            _updateTop(player, score);
        }

        if (escaped) {
            totalEscapes++;
            if (badgeOf[player] == 0) {
                uint256 tokenId = _nextTokenId++;
                badgeOf[player] = tokenId;
                _mint(player, tokenId);
                emit BadgeMinted(player, tokenId);
            }
        }

        emit RunSubmitted(player, score, escaped);
    }

    /// @notice Top scores, highest first (one entry per player).
    function getTop() external view returns (Entry[] memory) {
        return _top;
    }

    function domainSeparator() external view returns (bytes32) {
        return _domainSeparatorV4();
    }

    function tokenURI(uint256 tokenId) public view override returns (string memory) {
        _requireOwned(tokenId);
        string memory id = Strings.toString(tokenId);
        string memory svg = string.concat(
            '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 300">',
            '<rect width="300" height="300" fill="#1a0d08"/>',
            '<rect y="210" width="300" height="90" fill="#ff5a1f"/>',
            '<text x="150" y="120" font-family="monospace" font-size="36" fill="#ffd36e" text-anchor="middle">ESCAPED</text>',
            '<text x="150" y="165" font-family="monospace" font-size="16" fill="#ffffff" text-anchor="middle">Dig Up #',
            id,
            "</text></svg>"
        );
        string memory json = string.concat(
            '{"name":"Dig Up: Escaped #',
            id,
            '","description":"Escaped the rising lava in Dig Up on Hemi.","image":"data:image/svg+xml;base64,',
            Base64.encode(bytes(svg)),
            '"}'
        );
        return string.concat("data:application/json;base64,", Base64.encode(bytes(json)));
    }

    /// @dev Badges are soulbound: minting is allowed, transfers are not.
    function _update(address to, uint256 tokenId, address auth) internal override returns (address) {
        if (_ownerOf(tokenId) != address(0)) revert Soulbound();
        return super._update(to, tokenId, auth);
    }

    /// @dev Keeps `_top` sorted descending with at most TOP_SIZE entries and one entry per player.
    function _updateTop(address player, uint32 score) private {
        uint256 len = _top.length;
        uint256 i = len;
        for (uint256 j; j < len; j++) {
            if (_top[j].player == player) {
                i = j;
                break;
            }
        }
        if (i == len) {
            if (len < TOP_SIZE) {
                _top.push();
                len++;
            } else if (score <= _top[len - 1].score) {
                return;
            }
            i = len - 1;
        }
        while (i > 0 && _top[i - 1].score < score) {
            _top[i] = _top[i - 1];
            i--;
        }
        _top[i] = Entry(player, score, uint64(block.timestamp));
    }
}
