## General notes
* Use of seperation-of-concern (SoC) for UI (PL), logic and networking (BL), & local save data (DL)
* Save data will be
    1. User information, here own display name & public key
    2. Contact list, here each contact's display name, user-set nickname, & their public key
* Users can do peer discovery by broadcasting across their contact info over LAN so other LanChat users can discover them

## Program structure & layout
* CRUD framework utilized for both the local user & contacts in contact list
* SoC 3-layer architecture utilized for simplicity
* Peer-to-peer (P2P) networking over LAN for communication with messaging peers
* Diffie-Hellman (DH) key exchange handshake for encrypted messaging