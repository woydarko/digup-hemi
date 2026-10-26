// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import {ERC721} from "@openzeppelin/contracts/token/ERC721/ERC721.sol";
import {EIP712} from "@openzeppelin/contracts/utils/cryptography/EIP712.sol";
import {SignatureChecker} from "@openzeppelin/contracts/utils/cryptography/SignatureChecker.sol";
import {Ownable} from "@openzeppelin/contracts/access/Ownable.sol";
import {Base64} from "@openzeppelin/contracts/utils/Base64.sol";
import {Strings} from "@openzeppelin/contracts/utils/Strings.sol";

/// @title Dig Up on Hemi
/// @notice Onchain runs for the Dig Up game:
///  - gasless score submission: the player signs an EIP-712 `Run` (EOA or ERC-1271 wallet), and only the game
///    relayer may send it, after its off-chain checks
///  - an onchain top-10 leaderboard; the owner can ban a cheating wallet and remove it from the board
///  - a soulbound (ERC-5192) "Escaped" badge for players who reach the finish flag
/// @dev The game runs in the browser, so a score cannot be proven onchain. The relayer is the trust point:
///      it rate-limits and rejects implausible runs, and `ban` cleans up anything that gets through.
contract DigUp is ERC721, EIP712, Ownable {
    struct Entry {
        address player;
        uint32 score;
        uint64 timestamp;
    }

    bytes32 public constant RUN_TYPEHASH =
        keccak256("Run(address player,uint32 score,bool escaped,uint256 nonce,uint256 deadline)");

    uint256 public constant TOP_SIZE = 10;
    uint32 public constant MAX_SCORE = 1_000_000;
    /// @dev A signed run must land soon after it is signed, so nobody can hold it and submit it much later.
    uint256 public constant MAX_DEADLINE_WINDOW = 1 hours;
    bytes4 private constant ERC5192_INTERFACE_ID = 0xb45a3c0e;

    address public relayer;
    mapping(address => uint256) public nonces;
    mapping(address => uint32) public bestScore;
    mapping(address => uint256) public badgeOf; // 0 = no badge
    mapping(address => bool) public banned;

    uint256 public totalRuns;
    uint256 public totalEscapes;

    uint256 private _nextTokenId = 1;
    Entry[] private _top;

    event RunSubmitted(address indexed player, uint32 score, bool escaped);
    event BadgeMinted(address indexed player, uint256 indexed tokenId);
    event RelayerChanged(address indexed relayer);
    event PlayerBanned(address indexed player);
    /// @notice ERC-5192: the token is locked to its owner.
    event Locked(uint256 tokenId);

    error NotRelayer();
    error Expired();
    error DeadlineTooFar();
    error BadSignature();
    error ScoreTooHigh();
    error InvalidRun();
    error Banned();
    error Soulbound();

    constructor(address relayer_) ERC721("Dig Up: Escaped", "DIGUP") EIP712("DigUp", "1") Ownable(msg.sender) {
        relayer = relayer_;
        emit RelayerChanged(relayer_);
    }

    function setRelayer(address relayer_) external onlyOwner {
        relayer = relayer_;
        emit RelayerChanged(relayer_);
    }

    /// @notice Record a run signed by `player`. Only the relayer sends it, so the player never pays gas.
    function submitRun(address player, uint32 score, bool escaped, uint256 deadline, bytes calldata signature)
        external
    {
        if (msg.sender != relayer) revert NotRelayer();
        if (block.timestamp > deadline) revert Expired();
        if (deadline > block.timestamp + MAX_DEADLINE_WINDOW) revert DeadlineTooFar();
        if (score > MAX_SCORE) revert ScoreTooHigh();
        if (escaped && score == 0) revert InvalidRun(); // a finished run always has time score
        if (banned[player]) revert Banned();

        uint256 nonce = nonces[player]++;
        bytes32 digest = _hashTypedDataV4(keccak256(abi.encode(RUN_TYPEHASH, player, score, escaped, nonce, deadline)));
        if (!SignatureChecker.isValidSignatureNow(player, digest, signature)) revert BadSignature();

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
                emit Locked(tokenId);
                emit BadgeMinted(player, tokenId);
            }
        }

        emit RunSubmitted(player, score, escaped);
    }

    /// @notice Remove a cheating wallet from the leaderboard and reject its later runs.
    function ban(address player) external onlyOwner {
        banned[player] = true;
        bestScore[player] = 0;
        uint256 len = _top.length;
        for (uint256 i; i < len; i++) {
            if (_top[i].player == player) {
                for (uint256 j = i; j + 1 < len; j++) {
                    _top[j] = _top[j + 1];
                }
                _top.pop();
                break;
            }
        }
        emit PlayerBanned(player);
    }

    /// @notice Top scores, highest first (one entry per player).
    function getTop() external view returns (Entry[] memory) {
        return _top;
    }

    function domainSeparator() external view returns (bytes32) {
        return _domainSeparatorV4();
    }

    /// @notice ERC-5192: every badge is locked.
    function locked(uint256 tokenId) external view returns (bool) {
        _requireOwned(tokenId);
        return true;
    }

    function supportsInterface(bytes4 interfaceId) public view override returns (bool) {
        return interfaceId == ERC5192_INTERFACE_ID || super.supportsInterface(interfaceId);
    }

    /// @dev Approvals are useless on a soulbound token; rejecting them stops marketplaces from listing it.
    function approve(address, uint256) public pure override {
        revert Soulbound();
    }

    function setApprovalForAll(address, bool) public pure override {
        revert Soulbound();
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
