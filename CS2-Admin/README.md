## CS2-ADMIN

# Info
An admin command plugin with mysql support

# Commands
```js
//admin
♦ css_addadmin <steamid> <group> <immunity>
♦ css_removeadmin <steamid>

//baseban
♦ css_ban <player> <time> <reason>
♦ css_unban <steamid>

//basechat
♦ css_say <message> - sends message to all players
♦ css_csay <message> - sends centered message to all players
♦ css_dsay <message> - <message> - sends hud message to all players
♦ css_hsay <message> - <message>  - sends hud message to all players
♦ css_asay <message> - <message> - sends message to admins
♦ css_chat <message> - <message> - sends message to admins
♦ css_psay <#userid|name> <message> - sends private message

//basecomm
♦ css_mute <#userid|name|all @ commands>
♦ css_unmute <#userid|name|all @ commands>
♦ css_gag <#userid|name|all @ commands>
♦ css_ungag <message> - <#userid|name|all @ commands>
♦ css_silence <#userid|name|all @ commands>
♦ css_unsilence <#userid|name|all @ commands>
♦ css_tmute <#userid|name> <time> - Imposes a timed mute
♦ css_smute <#userid|name> <time> - Imposes a timed mute
♦ css_tunmute <#userid|name> - Unmute timed mute
♦ css_sunmute <#userid|name> - Unmute timed mute
♦ css_tgag <#userid|name> <time> - Imposes a timed gag
♦ css_sgag <#userid|name> <time> - Imposes a timed gag
♦ css_tungag <#userid|name> <time> - Ungag timed gag
♦ css_sungag <#userid|name> <time> - Ungag timed gag

//basecommands
♦ css_kick <#userid|name> <reason>
♦ css_changemap <map>
♦ css_map <map>
♦ css_workshop <map>
♦ css_wsmap <map>
♦ css_rcon <args>
♦ css_cvar <cvar> <value>
♦ css_exec <exec>
♦ css_who <#userid|name or empty for all>

//basevotes
♦ css_vote <question> [... Options ...]

//funcommands
♦ css_freeze <#userid|name|all @ commands> <time>
♦ css_unfreeze <#userid|name|all @ commands>
♦ css_gravity <gravity>
♦ css_revive <#userid|name|all @ commands>
♦ css_respawn <#userid|name|all @ commands>
♦ css_noclip <#userid|name|all @ commands> <value>
♦ css_weapon <#userid|name|all @ commands> <weapon>
♦ css_strip <#userid|name|all @ commands>
♦ css_sethp <team> <health> - Sets team players' spawn health
♦ css_hp <#userid|name|all @ commands> <health>
♦ css_speed <#userid|name|all @ commands> <value>
♦ css_god <#userid|name|all @ commands> <value>
♦ css_team <#userid|name|all @ commands> <value>
♦ css_swap <#userid|name>
♦ css_bury <#userid|name|all @ commands>
♦ css_unbury <#userid|name|all @ commands>
♦ css_clean - Clean weapons on the ground
♦ css_glow <#userid|name|all @ commands> <color>
♦ css_beacon <#userid|name|all @ commands> <value>

//playercommands
♦ css_slap <#userid|name|all @ commands> <damage>
♦ css_slay <#userid|name|all @ commands>
♦ css_rename <#userid|name> <newname>
```
