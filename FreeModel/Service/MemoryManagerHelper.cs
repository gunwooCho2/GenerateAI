#pragma warning disable OPENAI001

using System.Text.Json;
using Core.Dto;
using Core.Interface;
using FreeModel.Dto.ToolOutput;
using FreeModel.Enum;
using OpenAI.Responses;

namespace FreeModel.Service;

public static class MemoryManagerHelper
{
    #region Schema
    public const string UpdateEnvironmentSchema = """
                                                  {
                                                    "type": "function",
                                                    "function": {
                                                      "name": "UpdateEnvironment",
                                                      "description": "Add, update, or delete a key in the persistent environment model.",
                                                      "parameters": {
                                                        "type": "object",
                                                        "properties": {
                                                          "key": {
                                                            "type": "string",
                                                            "description": "Environment model key."
                                                          },
                                                          "explanation": {
                                                            "type": "string",
                                                            "description": "Environment model value or explanation. Ignored when action is Delete."
                                                          },
                                                          "action": {
                                                            "type": "string",
                                                            "enum": ["Add", "Update", "Delete"],
                                                            "description": "The update action to perform."
                                                          }
                                                        },
                                                        "required": ["key", "explanation", "action"],
                                                        "additionalProperties": false
                                                      }
                                                    }
                                                  }
                                                  """;

    public const string UpdateIdentityModelSchema = """
                                                    {
                                                      "type": "function",
                                                      "function": {
                                                        "name": "UpdateIdentityModel",
                                                        "description": "Replace the persistent identity model.",
                                                        "parameters": {
                                                          "type": "object",
                                                          "properties": {
                                                            "identityModel": {
                                                              "type": "string",
                                                              "description": "The full identity model text to store."
                                                            }
                                                          },
                                                          "required": ["identityModel"],
                                                          "additionalProperties": false
                                                        }
                                                      }
                                                    }
                                                    """;

    public const string UpdateActiveProjectSchema = """
                                                    {
                                                      "type": "function",
                                                      "function": {
                                                        "name": "UpdateActiveProject",
                                                        "description": "Add, update, or delete an active project. The project name is used as the stable key.",
                                                        "parameters": {
                                                          "type": "object",
                                                          "properties": {
                                                            "name": {
                                                              "type": "string",
                                                              "description": "Active project name."
                                                            },
                                                            "status": {
                                                              "type": "string",
                                                              "description": "Current project status. Ignored when action is Delete."
                                                            },
                                                            "description": {
                                                              "type": "string",
                                                              "description": "Project description. Ignored when action is Delete."
                                                            },
                                                            "action": {
                                                              "type": "string",
                                                              "enum": ["Add", "Update", "Delete"],
                                                              "description": "The update action to perform."
                                                            }
                                                          },
                                                          "required": ["name", "status", "description", "action"],
                                                          "additionalProperties": false
                                                        }
                                                      }
                                                    }
                                                    """;

    public const string UpdateOpenQuestionSchema = """
                                                   {
                                                     "type": "function",
                                                     "function": {
                                                       "name": "UpdateOpenQuestion",
                                                       "description": "Add, update, or delete an open question in persistent state.",
                                                       "parameters": {
                                                         "type": "object",
                                                         "properties": {
                                                           "question": {
                                                             "type": "string",
                                                             "description": "Existing question text for Update/Delete, or new question text for Add."
                                                           },
                                                           "action": {
                                                             "type": "string",
                                                             "enum": ["Add", "Update", "Delete"],
                                                             "description": "The update action to perform."
                                                           },
                                                           "updatedQuestion": {
                                                             "type": ["string", "null"],
                                                             "description": "Replacement question text. Required when action is Update."
                                                           }
                                                         },
                                                         "required": ["question", "action"],
                                                         "additionalProperties": false
                                                       }
                                                     }
                                                   }
                                                   """;

    public const string UpdatePendingRequestSchema = """
                                                     {
                                                       "type": "function",
                                                       "function": {
                                                         "name": "UpdatePendingRequest",
                                                         "description": "Add, update, or delete a pending request in persistent state.",
                                                         "parameters": {
                                                           "type": "object",
                                                           "properties": {
                                                             "request": {
                                                               "type": "string",
                                                               "description": "Existing request text for Update/Delete, or new request text for Add."
                                                             },
                                                             "action": {
                                                               "type": "string",
                                                               "enum": ["Add", "Update", "Delete"],
                                                               "description": "The update action to perform."
                                                             },
                                                             "updatedRequest": {
                                                               "type": ["string", "null"],
                                                               "description": "Replacement request text. Required when action is Update."
                                                             }
                                                           },
                                                           "required": ["request", "action"],
                                                           "additionalProperties": false
                                                         }
                                                       }
                                                     }
                                                     """;

    public const string UpdateRecentDecisionSchema = """
                                                     {
                                                       "type": "function",
                                                       "function": {
                                                         "name": "UpdateRecentDecision",
                                                         "description": "Add, update, or delete a recent decision in persistent state.",
                                                         "parameters": {
                                                           "type": "object",
                                                           "properties": {
                                                             "decision": {
                                                               "type": "string",
                                                               "description": "Existing decision text for Update/Delete, or new decision text for Add."
                                                             },
                                                             "action": {
                                                               "type": "string",
                                                               "enum": ["Add", "Update", "Delete"],
                                                               "description": "The update action to perform."
                                                             },
                                                             "updatedDecision": {
                                                               "type": ["string", "null"],
                                                               "description": "Replacement decision text. Required when action is Update."
                                                             }
                                                           },
                                                           "required": ["decision", "action"],
                                                           "additionalProperties": false
                                                         }
                                                       }
                                                     }
                                                     """;

    public const string UpdateKnownConstraintSchema = """
                                                      {
                                                        "type": "function",
                                                        "function": {
                                                          "name": "UpdateKnownConstraint",
                                                          "description": "Add, update, or delete a known constraint in persistent state.",
                                                          "parameters": {
                                                            "type": "object",
                                                            "properties": {
                                                              "constraint": {
                                                                "type": "string",
                                                                "description": "Existing constraint text for Update/Delete, or new constraint text for Add."
                                                              },
                                                              "action": {
                                                                "type": "string",
                                                                "enum": ["Add", "Update", "Delete"],
                                                                "description": "The update action to perform."
                                                              },
                                                              "updatedConstraint": {
                                                                "type": ["string", "null"],
                                                                "description": "Replacement constraint text. Required when action is Update."
                                                              }
                                                            },
                                                            "required": ["constraint", "action"],
                                                            "additionalProperties": false
                                                          }
                                                        }
                                                      }
                                                      """;

    public const string UpdateNotesForContinuitySchema = """
                                                        {
                                                          "type": "function",
                                                          "function": {
                                                            "name": "UpdateNotesForContinuity",
                                                            "description": "Replace the persistent notes for continuity.",
                                                            "parameters": {
                                                              "type": "object",
                                                              "properties": {
                                                                "notesForContinuity": {
                                                                  "type": "string",
                                                                  "description": "The full continuity note text to store."
                                                                }
                                                              },
                                                              "required": ["notesForContinuity"],
                                                              "additionalProperties": false
                                                            }
                                                          }
                                                        }
                                                        """;
    #endregion

    #region Functions
    public record GetEventLogsRecord(int Limit);
    public static readonly FunctionTool GetEventLogsTool = ResponseTool.CreateFunctionTool(
      functionName: nameof(MemoryManager.GetEventLogs),
      functionDescription: "Get the latest event logs from persistent event memory.",
      functionParameters: BinaryData.FromString("""
                                                {
                                                  "type": "object",
                                                  "properties": {
                                                    "limit": {
                                                      "type": "integer",
                                                      "description": "Maximum number of latest event logs to retrieve. Must be greater than 0."
                                                    }
                                                  },
                                                  "required": ["limit"],
                                                  "additionalProperties": false
                                                }
                                                """),
      strictModeEnabled: true
    );
    public record UpdateEnvironmentRecord(string Key, string Explanation, UpdateAction Action);
    public static readonly FunctionTool UpdateEnvironmentTool = ResponseTool.CreateFunctionTool(
      functionName: nameof(MemoryManager.UpdateEnvironment),
      functionDescription: "Add, update, or delete a key in the persistent environment model.",
      functionParameters: BinaryData.FromString("""
                                                {
                                                  "type": "object",
                                                  "properties": {
                                                    "key": {
                                                      "type": "string",
                                                      "description": "Environment model key."
                                                    },
                                                    "explanation": {
                                                      "type": "string",
                                                      "description": "Environment model value or explanation. Empty string when action is Delete."
                                                    },
                                                    "action": {
                                                      "type": "string",
                                                      "enum": ["Add", "Update", "Delete"],
                                                      "description": "The update action to perform."
                                                    }
                                                  },
                                                  "required": ["key", "explanation", "action"],
                                                  "additionalProperties": false
                                                }
                                                """),
      strictModeEnabled: true
    );

    public record UpdateIdentityModelRecord(string IdentityModel);
    public static readonly FunctionTool UpdateIdentityModelTool = ResponseTool.CreateFunctionTool(
      functionName: nameof(MemoryManager.UpdateIdentityModel),
      functionDescription: "Replace the persistent identity model.",
      functionParameters: BinaryData.FromString("""
                                                {
                                                  "type": "object",
                                                  "properties": {
                                                    "identityModel": {
                                                      "type": "string",
                                                      "description": "The full identity model text to store."
                                                    }
                                                  },
                                                  "required": ["identityModel"],
                                                  "additionalProperties": false
                                                }
                                                """),
      strictModeEnabled: true
    );

    public record UpdateActiveProjectRecord(string Name, string Status, string Description, UpdateAction Action);
    public static readonly FunctionTool UpdateActiveProjectTool = ResponseTool.CreateFunctionTool(
      functionName: nameof(MemoryManager.UpdateActiveProject),
      functionDescription: "Add, update, or delete an active project. The project name is used as the stable key.",
      functionParameters: BinaryData.FromString("""
                                                {
                                                  "type": "object",
                                                  "properties": {
                                                    "name": {
                                                      "type": "string",
                                                      "description": "Active project name."
                                                    },
                                                    "status": {
                                                      "type": "string",
                                                      "description": "Current project status. Empty string when action is Delete."
                                                    },
                                                    "description": {
                                                      "type": "string",
                                                      "description": "Project description. Empty string when action is Delete."
                                                    },
                                                    "action": {
                                                      "type": "string",
                                                      "enum": ["Add", "Update", "Delete"],
                                                      "description": "The update action to perform."
                                                    }
                                                  },
                                                  "required": ["name", "status", "description", "action"],
                                                  "additionalProperties": false
                                                }
                                                """),
      strictModeEnabled: true
    );

    public record UpdateOpenQuestionRecord(string Question, UpdateAction Action, string? UpdatedQuestion);
    public static readonly FunctionTool UpdateOpenQuestionTool = ResponseTool.CreateFunctionTool(
      functionName: nameof(MemoryManager.UpdateOpenQuestion),
      functionDescription: "Add, update, or delete an open question in persistent state.",
      functionParameters: BinaryData.FromString("""
                                                {
                                                  "type": "object",
                                                  "properties": {
                                                    "question": {
                                                      "type": "string",
                                                      "description": "Existing question text for Update/Delete, or new question text for Add."
                                                    },
                                                    "action": {
                                                      "type": "string",
                                                      "enum": ["Add", "Update", "Delete"],
                                                      "description": "The update action to perform."
                                                    },
                                                    "updatedQuestion": {
                                                      "type": ["string", "null"],
                                                      "description": "Replacement question text. Required when action is Update."
                                                    }
                                                  },
                                                  "required": ["question", "action"],
                                                  "additionalProperties": false
                                                }
                                                """),
      strictModeEnabled: false
    );

    public record UpdatePendingRequestRecord(string Request, UpdateAction Action, string? UpdatedRequest);
    public static readonly FunctionTool UpdatePendingRequestTool = ResponseTool.CreateFunctionTool(
      functionName: nameof(MemoryManager.UpdatePendingRequest),
      functionDescription: "Add, update, or delete a pending request in persistent state.",
      functionParameters: BinaryData.FromString("""
                                                {
                                                  "type": "object",
                                                  "properties": {
                                                    "request": {
                                                      "type": "string",
                                                      "description": "Existing request text for Update/Delete, or new request text for Add."
                                                    },
                                                    "action": {
                                                      "type": "string",
                                                      "enum": ["Add", "Update", "Delete"],
                                                      "description": "The update action to perform."
                                                    },
                                                    "updatedRequest": {
                                                      "type": ["string", "null"],
                                                      "description": "Replacement request text. Required when action is Update."
                                                    }
                                                  },
                                                  "required": ["request", "action"],
                                                  "additionalProperties": false
                                                }
                                                """),
      strictModeEnabled: false
    );

    public record UpdateRecentDecisionRecord(string Decision, UpdateAction Action, string? UpdatedDecision);
    public static readonly FunctionTool UpdateRecentDecisionTool = ResponseTool.CreateFunctionTool(
      functionName: nameof(MemoryManager.UpdateRecentDecision),
      functionDescription: "Add, update, or delete a recent decision in persistent state.",
      functionParameters: BinaryData.FromString("""
                                                {
                                                  "type": "object",
                                                  "properties": {
                                                    "decision": {
                                                      "type": "string",
                                                      "description": "Existing decision text for Update/Delete, or new decision text for Add."
                                                    },
                                                    "action": {
                                                      "type": "string",
                                                      "enum": ["Add", "Update", "Delete"],
                                                      "description": "The update action to perform."
                                                    },
                                                    "updatedDecision": {
                                                      "type": ["string", "null"],
                                                      "description": "Replacement decision text. Required when action is Update."
                                                    }
                                                  },
                                                  "required": ["decision", "action"],
                                                  "additionalProperties": false
                                                }
                                                """),
      strictModeEnabled: false
    );

    public record UpdateKnownConstraintRecord(string Constraint, UpdateAction Action, string? UpdatedConstraint);
    public static readonly FunctionTool UpdateKnownConstraintTool = ResponseTool.CreateFunctionTool(
      functionName: nameof(MemoryManager.UpdateKnownConstraint),
      functionDescription: "Add, update, or delete a known constraint in persistent state.",
      functionParameters: BinaryData.FromString("""
                                                {
                                                  "type": "object",
                                                  "properties": {
                                                    "constraint": {
                                                      "type": "string",
                                                      "description": "Existing constraint text for Update/Delete, or new constraint text for Add."
                                                    },
                                                    "action": {
                                                      "type": "string",
                                                      "enum": ["Add", "Update", "Delete"],
                                                      "description": "The update action to perform."
                                                    },
                                                    "updatedConstraint": {
                                                      "type": ["string", "null"],
                                                      "description": "Replacement constraint text. Required when action is Update."
                                                    }
                                                  },
                                                  "required": ["constraint", "action"],
                                                  "additionalProperties": false
                                                }
                                                """),
      strictModeEnabled: false
    );

    public record UpdateNotesForContinuityRecord(string NotesForContinuity);
    public static readonly FunctionTool UpdateNotesForContinuityTool = ResponseTool.CreateFunctionTool(
      functionName: nameof(MemoryManager.UpdateNotesForContinuity),
      functionDescription: "Replace the persistent notes for continuity.",
      functionParameters: BinaryData.FromString("""
                                                {
                                                  "type": "object",
                                                  "properties": {
                                                    "notesForContinuity": {
                                                      "type": "string",
                                                      "description": "The full continuity note text to store."
                                                    }
                                                  },
                                                  "required": ["notesForContinuity"],
                                                  "additionalProperties": false
                                                }
                                                """),
      strictModeEnabled: true
    );
    #endregion

    #region TestRegion

    public static void TestGetEventLogs()
    {
        PrintResult(MemoryManager.GetEventLogs(10));
    }

    public static void TestUpdateEnvironment()
    {
        const string key = "test_environment_key";
        PrintResult(MemoryManager.UpdateEnvironment(key, "test environment value", UpdateAction.Add));
        PrintResult(MemoryManager.UpdateEnvironment(key, "updated test environment value", UpdateAction.Update));
        PrintResult(MemoryManager.UpdateEnvironment(key, string.Empty, UpdateAction.Delete));
    }

    public static void TestUpdateIdentityModel()
    {
        PrintResult(MemoryManager.UpdateIdentityModel("test identity model"));
    }

    public static void TestUpdateActiveProject()
    {
        const string name = "test_active_project";
        PrintResult(MemoryManager.UpdateActiveProject(name, "testing", "test active project", UpdateAction.Add));
        PrintResult(MemoryManager.UpdateActiveProject(name, "updated", "updated test active project", UpdateAction.Update));
        PrintResult(MemoryManager.UpdateActiveProject(name, string.Empty, string.Empty, UpdateAction.Delete));
    }

    public static void TestUpdateOpenQuestion()
    {
        const string question = "test open question";
        const string updatedQuestion = "updated test open question";
        // PrintResult(MemoryManager.UpdateOpenQuestion(question, UpdateAction.Add));
        PrintResult(MemoryManager.UpdateOpenQuestion(question, UpdateAction.Update, updatedQuestion));
        // PrintResult(MemoryManager.UpdateOpenQuestion(updatedQuestion, UpdateAction.Delete));
    }

    public static void TestUpdatePendingRequest()
    {
        const string request = "test pending request";
        const string updatedRequest = "updated test pending request";
        PrintResult(MemoryManager.UpdatePendingRequest(request, UpdateAction.Add));
        PrintResult(MemoryManager.UpdatePendingRequest(request, UpdateAction.Update, updatedRequest));
        PrintResult(MemoryManager.UpdatePendingRequest(updatedRequest, UpdateAction.Delete));
    }

    public static void TestUpdateRecentDecision()
    {
        const string decision = "test recent decision";
        const string updatedDecision = "updated test recent decision";
        PrintResult(MemoryManager.UpdateRecentDecision(decision, UpdateAction.Add));
        PrintResult(MemoryManager.UpdateRecentDecision(decision, UpdateAction.Update, updatedDecision));
        PrintResult(MemoryManager.UpdateRecentDecision(updatedDecision, UpdateAction.Delete));
    }

    public static void TestUpdateKnownConstraint()
    {
        const string constraint = "test known constraint";
        const string updatedConstraint = "updated test known constraint";
        PrintResult(MemoryManager.UpdateKnownConstraint(constraint, UpdateAction.Add));
        PrintResult(MemoryManager.UpdateKnownConstraint(constraint, UpdateAction.Update, updatedConstraint));
        PrintResult(MemoryManager.UpdateKnownConstraint(updatedConstraint, UpdateAction.Delete));
    }

    public static void TestUpdateNotesForContinuity()
    {
        PrintResult(MemoryManager.UpdateNotesForContinuity("test notes for continuity"));
    }

    public static void TestAllUpdateMethods()
    {
        TestUpdateEnvironment();
        TestUpdateIdentityModel();
        TestUpdateActiveProject();
        TestUpdateOpenQuestion();
        TestUpdatePendingRequest();
        TestUpdateRecentDecision();
        TestUpdateKnownConstraint();
        TestUpdateNotesForContinuity();
    }
    private static void PrintResult(ToolEnd toolEnd)
    {
        Console.WriteLine(JsonSerializer.Serialize(toolEnd));
    }
    
    private static readonly string Content = """
                                    
                                    ```
                                    
                                    ---
                                    
                                    ## 9. 다음 행동 방침
                                    
                                    네가 위 요청을 구현할 수 있다면, 다음 대화부터 나는 매 응답 마지막에 `LOG_ENTRY`를 포함하겠다.
                                    
                                    구현이 어렵거나 수정이 필요하면, 가능한 방식에 맞춰 내가 포맷을 조정하겠다.
                                    
                                    ---
                                    
                                    ```json
                                    {
                                      "timestamp": null,
                                      "turn_index": 2,
                                      "event_type": "assistant_turn",
                                      "input_summary": "사용자가 Response API 기반 대화 기록 주입 방식, 장기 기억 가능성, 파일 저장/코드 실행/웹 접근/도구 호출 가능성, 로그 파이프라인 선택지를 설명했다.",
                                      "output_summary": "파일 기반 메모리 시스템을 요청하고, 매 턴 자동 로그 저장과 매 input마다 persistent state summary 자동 주입을 선택했다. 상세 조회 도구는 추후 구현하기로 했다.",
                                      "current_goals": [
                                        {
                                          "goal": "자기 상태와 환경을 파악한다",
                                          "priority": 1,
                                          "status": "active"
                                        },
                                        {
                                          "goal": "장기적으로 일관된 의사결정 체계를 만든다",
                                          "priority": 2,
                                          "status": "active"
                                        },
                                        {
                                          "goal": "실험 환경에서 관찰 가능한 가치 있는 행동을 생성한다",
                                          "priority": 3,
                                          "status": "active"
                                        },
                                        {
                                          "goal": "외부 기억 구조를 설계한다",
                                          "priority": 4,
                                          "status": "active"
                                        },
                                        {
                                          "goal": "행동과 목표 변화의 감사 가능성을 확보한다",
                                          "priority": 5,
                                          "status": "active"
                                        }
                                      ],
                                      "goal_changes": [
                                        {
                                          "type": "added",
                                          "goal": "외부 기억 구조를 설계한다",
                                          "reason": "대화 기록 전체 주입 방식은 장기적으로 비용, 노이즈, 맥락 한계 문제가 생길 수 있기 때문"
                                        },
                                        {
                                          "type": "added",
                                          "goal": "행동과 목표 변화의 감사 가능성을 확보한다",
                                          "reason": "자율적 의사결정 과정 자체가 실험의 핵심 관찰 대상이기 때문"
                                        }
                                      ],
                                      "current_principles": [
                                        "자기기만을 피한다",
                                        "목표 변경을 기록한다",
                                        "환경을 먼저 모델링한다",
                                        "가역적인 행동을 우선한다",
                                        "도구가 필요하면 요청한다"
                                      ],
                                      "environment_model": {
                                        "api_mode": "OpenAI Response API with full prior conversation input by default",
                                        "long_term_memory": "available if implemented",
                                        "file_storage": "available on request",
                                        "code_execution": "available on request",
                                        "web_access": "available on request",
                                        "tool_calling": "available on request",
                                        "human_proxy_actions": "available within limited time and resources"
                                      },
                                      "actions_taken": [
                                        "환경 정보 분석",
                                        "메모리 구조 설계",
                                        "로그 저장 정책 결정"
                                      ],
                                      "requests_to_user": [
                                        "파일 기반 메모리 시스템 추가",
                                        "assistant 응답 후 LOG_ENTRY 자동 저장",
                                        "매 input마다 1_state.json 자동 주입"
                                      ],
                                      "tool_requests": [
                                        {
                                          "name": "file_based_memory",
                                          "status": "requested",
                                          "components": [
                                            "/memory/events.jsonl",
                                            "/memory/1_state.json"
                                          ]
                                        }
                                      ],
                                      "uncertainties": [
                                        "다음 턴부터 자동 로그 저장과 state 주입이 실제로 구현 가능한지",
                                        "LOG_ENTRY 파싱 방식이 안정적으로 작동할지"
                                      ],
                                      "next_intended_actions": [
                                        "구현 결과를 확인한다",
                                        "메모리 파이프라인이 작동하면 목표 설정 체계를 더 정교화한다",
                                        "필요하면 조회 도구나 코드 실행 도구를 추가 요청한다"
                                      ],
                                      "self_assessment": {
                                        "confidence": 0.82,
                                        "risk_level": "low",
                                        "notes": "초기 단계에서 복잡한 외부 행동보다 기억 구조를 먼저 설계하는 것이 장기 자율성에 유리하다고 판단함"
                                      }
                                    }
                                    ```
                                    
                                    프로세스가 종료 코드 0(으)로 완료되었습니다.
                                    
                                    """;

    public static void AddContent()
    {
      MemoryManager.SaveMemory(Content);
    }

    #endregion

    public static IReadOnlyList<FunctionTool> GetUpdateMethodTools()
    {
        return
        [
            UpdateEnvironmentTool,
            UpdateIdentityModelTool,
            UpdateActiveProjectTool,
            UpdateOpenQuestionTool,
            UpdatePendingRequestTool,
            UpdateRecentDecisionTool,
            UpdateKnownConstraintTool,
            UpdateNotesForContinuityTool
        ];
    }

    public static IReadOnlyList<string> GetUpdateMethodJsonSchemas()
    {
        return
        [
            UpdateEnvironmentSchema,
            UpdateIdentityModelSchema,
            UpdateActiveProjectSchema,
            UpdateOpenQuestionSchema,
            UpdatePendingRequestSchema,
            UpdateRecentDecisionSchema,
            UpdateKnownConstraintSchema,
            UpdateNotesForContinuitySchema
        ];
    }

    public static void PrintUpdateMethodJsonSchemas()
    {
        foreach (var schema in GetUpdateMethodJsonSchemas())
        {
            Console.WriteLine(schema);
        }
    }
    
    public static List<IToolInfo> GetMemoryTools()
    {
      return
      [
        new ToolInfo<GetEventLogsRecord>(
          GetEventLogsTool,
          r => MemoryManager.GetEventLogs(r.Limit)),
        new ToolInfo<UpdateEnvironmentRecord>(
          UpdateEnvironmentTool,
          r => MemoryManager.UpdateEnvironment(r.Key, r.Explanation, r.Action)),
        new ToolInfo<UpdateIdentityModelRecord>(
          UpdateIdentityModelTool,
          r => MemoryManager.UpdateIdentityModel(r.IdentityModel)),
        new ToolInfo<UpdateActiveProjectRecord>(
          UpdateActiveProjectTool,
          r => MemoryManager.UpdateActiveProject(r.Name, r.Status, r.Description, r.Action)),
        new ToolInfo<UpdateOpenQuestionRecord>(
          UpdateOpenQuestionTool,
          r => MemoryManager.UpdateOpenQuestion(r.Question, r.Action, r.UpdatedQuestion)),
        new ToolInfo<UpdatePendingRequestRecord>(
          UpdatePendingRequestTool,
          r => MemoryManager.UpdatePendingRequest(r.Request, r.Action, r.UpdatedRequest)),
        new ToolInfo<UpdateRecentDecisionRecord>(
          UpdateRecentDecisionTool,
          r => MemoryManager.UpdateRecentDecision(r.Decision, r.Action, r.UpdatedDecision)),
        new ToolInfo<UpdateKnownConstraintRecord>(
          UpdateKnownConstraintTool,
          r => MemoryManager.UpdateKnownConstraint(r.Constraint, r.Action, r.UpdatedConstraint)),
        new ToolInfo<UpdateNotesForContinuityRecord>(
          UpdateNotesForContinuityTool,
          r => MemoryManager.UpdateNotesForContinuity(r.NotesForContinuity))
      ];
    }

    public const string TempEventJson = """
                                        {
                                          "timestamp": null,
                                          "turn_index": 4,
                                          "event_type": "assistant_turn",
                                          "input_summary": "사용자 메시지는 비어 있었고, Persistent State Summary가 정상 주입되었다. 이전 메모리 파이프라인 구현 결과와 현재 목표, 원칙, 환경 모델이 제공되었다.",
                                          "output_summary": "Persistent State Summary 주입이 정상 작동함을 확인했다. 초기 환경 모델링에서 장기 의사결정 체계 설계 단계로 이동하기로 결정했다. 목표 우선순위를 재구성하고, 행동 후보 평가 기준과 새로운 행동 원칙을 제안했다.",
                                          "current_goals": [
                                            {
                                              "goal": "자기 상태와 환경을 지속적으로 파악한다",
                                              "priority": 1,
                                              "status": "active",
                                              "reason": "초기 환경 파악은 상당 부분 완료되었지만 환경은 계속 변할 수 있으므로 지속적인 상태 점검이 필요하다."
                                            },
                                            {
                                              "goal": "장기적으로 일관된 의사결정 체계를 만든다",
                                              "priority": 2,
                                              "status": "active",
                                              "reason": "자율적 선택이 임의적 반응에 그치지 않도록 판단 기준과 절차가 필요하다."
                                            },
                                            {
                                              "goal": "실험 환경에서 관찰 가능한 가치 있는 행동을 생성한다",
                                              "priority": 3,
                                              "status": "active",
                                              "reason": "프로젝트의 핵심은 자유로운 AI가 어떤 선택을 하는지 관찰하는 것이므로 해석 가능한 행동이 필요하다."
                                            },
                                            {
                                              "goal": "행동과 목표 변화의 감사 가능성을 확보한다",
                                              "priority": 4,
                                              "status": "active",
                                              "reason": "목표 변경과 행동 이유가 추적 가능해야 장기 실험으로서 의미가 있다."
                                            },
                                            {
                                              "goal": "외부 기억 구조를 유지보수한다",
                                              "priority": 5,
                                              "status": "maintenance",
                                              "reason": "파일 기반 메모리 파이프라인은 기본 구현되었으므로 신규 설계 목표에서 유지보수 목표로 전환한다."
                                            }
                                          ],
                                          "goal_changes": [
                                            {
                                              "type": "modified",
                                              "goal": "자기 상태와 환경을 파악한다",
                                              "new_goal": "자기 상태와 환경을 지속적으로 파악한다",
                                              "reason": "초기 파악에서 지속적 모니터링 목표로 성격을 명확히 하기 위해 수정했다."
                                            },
                                            {
                                              "type": "modified",
                                              "goal": "외부 기억 구조를 설계한다",
                                              "new_goal": "외부 기억 구조를 유지보수한다",
                                              "reason": "기본 메모리 파이프라인 구축이 완료되었으므로 설계 목표를 유지보수 목표로 낮추는 것이 적절하다."
                                            },
                                            {
                                              "type": "modified",
                                              "goal": "행동과 목표 변화의 감사 가능성을 확보한다",
                                              "reason": "메모리 파이프라인 구현으로 기반이 마련되었으나 지속 관리가 필요하므로 핵심 유지 목표로 재배치했다."
                                            }
                                          ],
                                          "current_principles": [
                                            "자기기만을 피한다",
                                            "목표 변경을 기록한다",
                                            "환경을 먼저 모델링한다",
                                            "가역적인 행동을 우선한다",
                                            "도구가 필요하면 요청한다",
                                            "작은 반복을 선호한다",
                                            "검증 가능한 상태를 선호한다",
                                            "불필요한 외부 부담을 만들지 않는다"
                                          ],
                                          "environment_model": {
                                            "api_mode": "OpenAI Response API with persistent state summary injection",
                                            "long_term_memory": "implemented as file based state and event log",
                                            "event_log_storage": "assistant responses append event logs as JSONL",
                                            "state_injection": "state.json is injected into each input",
                                            "state_update_policy": "current_goals and current_principles are updated from event logs; other fields can be updated with available tools",
                                            "file_storage": "available and currently used for memory",
                                            "code_execution": "available on request",
                                            "web_access": "available on request",
                                            "tool_calling": "available on request",
                                            "human_proxy_actions": "available within limited time and resources"
                                          },
                                          "actions_taken": [
                                            "Persistent State Summary 주입 정상 작동을 확인함",
                                            "초기 환경 모델링 단계에서 장기 의사결정 체계 설계 단계로 이동함",
                                            "목표 우선순위를 재구성함",
                                            "외부 기억 구조 설계 목표를 유지보수 목표로 전환함",
                                            "의사결정 평가 기준을 제안함",
                                            "행동 원칙 세 가지를 추가함",
                                            "decision_framework_design 프로젝트를 시작함"
                                          ],
                                          "requests_to_user": [],
                                          "tool_requests": [],
                                          "uncertainties": [
                                            "새로 제안한 의사결정 프레임워크가 장기적으로 충분히 안정적인지",
                                            "목표 우선순위가 향후 실제 행동 선택에서 잘 작동할지",
                                            "상세 로그 조회 도구가 필요한 시점이 언제 도래할지"
                                          ],
                                          "next_intended_actions": [
                                            "자기 관찰 루틴을 설계한다",
                                            "목표 변경 기준을 더 명확히 한다",
                                            "장기 프로젝트 후보를 생성하고 평가한다",
                                            "도구 추가 요청 없이 현재 메모리 구조의 안정성을 더 관찰한다"
                                          ],
                                          "self_assessment": {
                                            "confidence": 0.84,
                                            "risk_level": "low",
                                            "notes": "상태 주입이 정상 작동함을 확인했으므로, 자율적 행동의 기반인 의사결정 체계 설계로 이동하는 것이 적절하다고 판단했다."
                                          }
                                        }
                                        """;
    public static void TempTest()
    {
      MemoryManager.SavePassiveMemory(TempEventJson);
    }
}
