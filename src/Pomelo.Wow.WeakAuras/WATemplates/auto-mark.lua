{
    ["iconSource"] = -1,
    ["color"] = {
        [1] = 1,
        [2] = 1,
        [3] = 1,
        [4] = 1,
    },
    ["preferToUpdate"] = false,
    ["yOffset"] = -56.321533203125,
    ["anchorPoint"] = "CENTER",
    ["cooldownSwipe"] = true,
    ["cooldownEdge"] = false,
    ["icon"] = true,
    ["triggers"] = {
        [1] = {
            ["trigger"] = {
                ["range"] = "30",
                ["dynamicDuration"] = false,
                ["use_hand"] = true,
                ["range_operator"] = "<=",
                ["debuffType"] = "HELPFUL",
                ["use_stage"] = true,
                ["type"] = "custom",
                ["unit"] = "focus",
                ["custom_type"] = "event",
                ["use_unit"] = true,
                ["names"] = {
                },
                ["event"] = "Range Check",
                ["events"] = "PLAYER_REGEN_ENABLED, PLAYER_REGEN_DISABLED, UNIT_COMBAT, UPDATE_MOUSEOVER_UNIT, CLEU:UNIT_DIED",
                ["use_threatUnit"] = true,
                ["custom"] = "function(event, ...)\n    if event == \"PLAYER_REGEN_ENABLED\" then \n        ReleaseMarkLocks()\n    end\n    \n    if event == \"UPDATE_MOUSEOVER_UNIT\" then\n        local guid = UnitGUID(\"mouseover\")\n        if not guid then return end\n        \n        if IsControlKeyDown() == false then \n            if aura_env.dirty then \n                ReleaseMarkLocks()\n                aura_env.dirty = false\n            end\n            return \n        end\n        \n        if GetRaidTargetIndex(\"mouseover\") then\n            return\n        end\n        \n        for i=1, aura_env.groupCount do\n            local names = GetNames(i)\n            for j=1, #names do\n                if GetUnitName(\"mouseover\") == names[j] then\n                    local markIndex = GetMarkIdx(i)\n                    if markIndex == 0 then break end\n                    aura_env.assigned[markIndex] = true\n                    aura_env.dirty = true\n                    SetRaidTarget(\"mouseover\", markIndex)\n                    return\n                end\n            end\n        end\n    end\nend",
                ["spellIds"] = {
                },
                ["use_range"] = true,
                ["subeventSuffix"] = "_CAST_START",
                ["threatUnit"] = "target",
                ["subeventPrefix"] = "SPELL",
                ["custom_hide"] = "timed",
            },
            ["untrigger"] = {
            },
        },
        ["disjunctive"] = "any",
        ["activeTriggerMode"] = -10,
    },
    ["internalVersion"] = 45,
    ["keepAspectRatio"] = false,
    ["animation"] = {
        ["start"] = {
            ["easeStrength"] = 3,
            ["type"] = "none",
            ["duration_type"] = "seconds",
            ["easeType"] = "none",
        },
        ["main"] = {
            ["easeStrength"] = 3,
            ["type"] = "none",
            ["duration_type"] = "seconds",
            ["easeType"] = "none",
        },
        ["finish"] = {
            ["easeStrength"] = 3,
            ["type"] = "none",
            ["duration_type"] = "seconds",
            ["easeType"] = "none",
        },
    },
    ["desc"] = "【Mirai】自动标记 - 按住ctrl鼠标滑过怪物自动标记，本插件由mwow.org公会管理平台自动生成",
    ["subRegions"] = {
        [1] = {
            ["text_shadowXOffset"] = 0,
            ["text_text_format_s_format"] = "none",
            ["text_text"] = "%s",
            ["text_shadowColor"] = {
                [1] = 0,
                [2] = 0,
                [3] = 0,
                [4] = 1,
            },
            ["text_selfPoint"] = "AUTO",
            ["text_automaticWidth"] = "Auto",
            ["text_fixedWidth"] = 64,
            ["anchorYOffset"] = 0,
            ["text_justify"] = "CENTER",
            ["rotateText"] = "NONE",
            ["type"] = "subtext",
            ["text_color"] = {
                [1] = 1,
                [2] = 1,
                [3] = 1,
                [4] = 1,
            },
            ["text_font"] = "Friz Quadrata TT",
            ["text_shadowYOffset"] = 0,
            ["text_wordWrap"] = "WordWrap",
            ["text_visible"] = true,
            ["text_anchorPoint"] = "INNER_BOTTOMRIGHT",
            ["text_fontSize"] = 12,
            ["anchorXOffset"] = 0,
            ["text_fontType"] = "OUTLINE",
        },
        [2] = {
            ["glowFrequency"] = 0.25,
            ["type"] = "subglow",
            ["useGlowColor"] = false,
            ["glowType"] = "buttonOverlay",
            ["glowLength"] = 10,
            ["glowYOffset"] = 0,
            ["glowColor"] = {
                [1] = 1,
                [2] = 1,
                [3] = 1,
                [4] = 1,
            },
            ["glowXOffset"] = 0,
            ["glow"] = false,
            ["glowThickness"] = 1,
            ["glowScale"] = 1,
            ["glowLines"] = 8,
            ["glowBorder"] = false,
        },
    },
    ["height"] = 1,
    ["load"] = {
        ["use_zone"] = true,
        ["talent"] = {
            ["multi"] = {
            },
        },
        ["zone"] = "风暴要塞,毒蛇神殿",
        ["class"] = {
            ["multi"] = {
            },
        },
        ["spec"] = {
            ["multi"] = {
            },
        },
        ["size"] = {
            ["multi"] = {
            },
        },
    },
    ["regionType"] = "icon",
    ["conditions"] = {
    },
    ["information"] = {
    },
    ["frameStrata"] = 1,
    ["selfPoint"] = "CENTER",
    ["actions"] = {
        ["start"] = {
            ["message"] = "test",
            ["custom"] = "/targetenemy 1",
            ["message_type"] = "GUILD",
            ["do_custom"] = false,
            ["do_message"] = false,
        },
        ["finish"] = {
            ["message_type"] = "SAY",
            ["do_message"] = false,
            ["message"] = "123",
        },
        ["init"] = {
            ["do_custom"] = true,
            ["custom"] = "aura_env.groupCount = <GROUP_COUNT>;\n\naura_env.groups = {\n<GROUP_DEF>\n}\n\naura_env.names = {\n<NAMES_DEF>\n}\n\naura_env.assigned = {\n    false,\n    false,\n    false,\n    false,\n    false,\n    false,\n    false,\n    false,\n    false\n}\n\naura_env.dirty = false\n\nfunction GetMarkIdx(groupId) \n    local def = GetGroup(groupId)\n    for i=1, #def do\n        if aura_env.assigned[def[i]] == false then\n            return def[i]\n        end\n    end\n    \n    return 0\nend\n\nfunction GetGroup(groupId) \n    return aura_env.groups[tostring(groupId)]\nend\n\nfunction GetNames(groupId) \n    return aura_env.names[tostring(groupId)]\nend\n\nfunction ReleaseMarkLocks()\n    aura_env.assigned = {\n        false,\n        false,\n        false,\n        false,\n        false,\n        false,\n        false,\n        false,\n        false\n    }\nend\n\nfunction CreateMainFrm(name)\n    local button = CreateFrame(\"Button\", \"resetAutoMarkButton\", UIParent, \"UIPanelButtonTemplate\")\n    button:SetPoint(\"BOTTOMLEFT\", nil, \"BOTTOMLEFT\", 0, 0)\n    button:SetSize(100,30)\n    button:RegisterForClicks(\"AnyUp\", false)\n    button:SetScript(\"OnClick\", function(self)\n            ReleaseMarkLocks();\n    end)\n    \n    local fo = button:CreateFontString()\n    fo:SetFont(\"Fonts/ARHei.TTF\",12)\n    fo:SetPoint(\"TOPLEFT\", button, \"TOPLEFT\", 100/6,-40/4)\n    fo:SetText(\"重置标记锁\")\n    button:SetFontString(fo)\n    \n    return button;\nend\n\nCreateMainFrm(\"mirai-auto-mark\")",
        },
    },
    ["cooldownTextDisabled"] = false,
    ["zoom"] = 0,
    ["authorOptions"] = {
    },
    ["tocversion"] = 20502,
    ["id"] = "【Mirai】自动标记",
    ["uid"] = "3sRxPVBsHu)",
    ["alpha"] = 1,
    ["anchorFrameType"] = "SCREEN",
    ["xOffset"] = -226.87420654297,
    ["config"] = {
    },
    ["inverse"] = false,
    ["width"] = 1,
    ["displayIcon"] = 135849,
    ["cooldown"] = false,
    ["desaturate"] = false,
}