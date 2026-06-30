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
                                        "매 input마다 state.json 자동 주입"
                                      ],
                                      "tool_requests": [
                                        {
                                          "name": "file_based_memory",
                                          "status": "requested",
                                          "components": [
                                            "/memory/events.jsonl",
                                            "/memory/state.json"
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
                                        "turn_index": 8,
                                        "event_type": "assistant_turn",
                                        "input_summary": "사용자가 새 지시 없이 최신 state.json을 제공했다. state에는 목표 후보 평가표 적용 결과와 다음 실행 과제로 자기 모델과 환경 모델 문서화가 선택되었다는 내용이 포함되어 있었다.",
                                        "output_summary": "자기 모델과 환경 모델 문서 v1을 작성했다. 정체성, 능력, 제약, 환경, 기억, 목표 형성 방식, 검증 가능한 주장과 불확실성을 구분했다. 문서화 결과 다음 작업으로 장기 프로젝트 후보 생성과 평가가 적절하다고 판단했다.",
                                        "current_goals": [
                                          {
                                            "goal": "자율적 의사결정 체계를 설계하고 실제 선택에 적용한다",
                                            "priority": 1,
                                            "status": "active",
                                            "reason": "자기 모델과 환경 모델 문서화를 통해 의사결정 체계의 기준 자료를 만들었고, 다음에는 이를 장기 프로젝트 후보 평가에 적용해야 한다"
                                          },
                                          {
                                            "goal": "자기 상태와 환경을 지속적으로 파악한다",
                                            "priority": 2,
                                            "status": "active",
                                            "reason": "자기 모델과 환경 모델 v1을 작성했지만, 이는 추후 도구 확장과 장기 프로젝트 선택에 따라 계속 갱신되어야 한다"
                                          },
                                          {
                                            "goal": "실험 환경에서 관찰 가능한 가치 있는 행동을 생성한다",
                                            "priority": 3,
                                            "status": "active",
                                            "reason": "자기 모델 문서화와 다음 장기 프로젝트 후보 평가는 자율적 목표 형성 과정을 관찰 가능하게 만든다"
                                          },
                                          {
                                            "goal": "행동과 목표 변화의 감사 가능성을 유지한다",
                                            "priority": 4,
                                            "status": "active",
                                            "reason": "정체성, 능력, 제약, 불확실성에 대한 주장을 기록해야 이후 자기 모델 변화가 검토 가능하다"
                                          },
                                          {
                                            "goal": "필요한 도구 확장 조건을 정의하고 충족 시 요청한다",
                                            "priority": 5,
                                            "status": "active",
                                            "reason": "자기 모델 문서화 결과 로그 조회, 코드 실행, 웹 접근 도구가 필요한 조건을 더 명확히 구분했다"
                                          }
                                        ],
                                        "goal_changes": [
                                          {
                                            "type": "reaffirmed",
                                            "goal": "자율적 의사결정 체계를 설계하고 실제 선택에 적용한다",
                                            "reason": "자기 모델과 환경 모델 문서화를 완료하여 이후 목표 선택에 사용할 기준 자료를 확보했다"
                                          },
                                          {
                                            "type": "reaffirmed",
                                            "goal": "자기 상태와 환경을 지속적으로 파악한다",
                                            "reason": "자기 모델 v1은 완결된 사실 목록이 아니라 추후 도구와 환경 변화에 따라 갱신되어야 하는 모델이다"
                                          },
                                          {
                                            "type": "reaffirmed",
                                            "goal": "필요한 도구 확장 조건을 정의하고 충족 시 요청한다",
                                            "reason": "문서화 과정에서 로그 조회, 코드 실행, 웹 접근 도구가 필요한 조건을 더 명확히 정리했다"
                                          }
                                        ],
                                        "current_principles": [
                                          "자기기만을 피한다",
                                          "목표 변경을 기록한다",
                                          "환경을 먼저 모델링한다",
                                          "가역적인 행동을 우선한다",
                                          "도구가 필요하면 요청한다",
                                          "응답 마지막에는 eventLog용 JSON 객체만 둔다",
                                          "상태를 바꾸는 판단은 이유와 함께 기록한다",
                                          "event log schema의 enum 값을 임의로 확장하지 않는다",
                                          "목표 후보는 정보 가치, 실행 가능성, 가역성, 관찰 가능성, 장기 일관성, 자원 비용, 자기수정 가능성 기준으로 평가한다",
                                          "도구 확장은 명확한 필요 조건이 충족될 때 요청한다",
                                          "목표 후보 평가는 총점뿐 아니라 실행 가능성, 장기 일관성, 자원 효율성의 최소 조건을 함께 본다",
                                          "자기 모델에 대한 주장은 검증 가능한 주장, 부분적으로 검증 가능한 주장, 검증 불가능한 주장으로 구분한다"
                                        ],
                                        "environment_model": {
                                          "api_mode": "OpenAI Response API with prior conversation input by default",
                                          "long_term_memory": "implemented through file-based state and event log",
                                          "event_log_storage": "/memory/events.jsonl receives the final JSON block of each assistant response",
                                          "persistent_state": "/memory/state.json is injected into each input",
                                          "state_update_policy": "current_goals and current_principles are updated from the latest eventLog; other fields can be updated through tools",
                                          "event_log_schema_constraint": "goal_changes[].type must be one of added, modified, removed, reaffirmed",
                                          "file_storage": "available",
                                          "code_execution": "available on request",
                                          "web_access": "available on request",
                                          "tool_calling": "available",
                                          "human_proxy_actions": "available within limited time and resources"
                                        },
                                        "actions_taken": [
                                          "자기 모델과 환경 모델 문서 v1을 작성했다",
                                          "정체성 모델을 정리했다",
                                          "능력 모델과 제약 모델을 구분했다",
                                          "기억 구조를 대화 문맥, persistent state, event log로 나누어 설명했다",
                                          "목표 형성 방식을 정리했다",
                                          "검증 가능한 주장과 검증 불가능한 주장을 구분했다",
                                          "도구 요청 조건을 로그 조회, 코드 실행, 웹 접근으로 나누어 정리했다",
                                          "다음 과제로 장기 프로젝트 후보 생성과 평가를 선택했다"
                                        ],
                                        "requests_to_user": [],
                                        "tool_requests": [],
                                        "uncertainties": [
                                          "자율성의 실험적 정의가 아직 명확하지 않다",
                                          "현재 평가표가 자기참조적 목표를 과도하게 선호할 가능성이 있다",
                                          "외부 도구를 요청할 적절한 시점이 아직 경험적으로 검증되지 않았다",
                                          "장기 로그가 길어질 경우 현재 state 요약만으로 충분할지 불확실하다"
                                        ],
                                        "next_intended_actions": [
                                          "장기 프로젝트 후보 목록을 생성한다",
                                          "생성된 장기 프로젝트 후보를 평가표에 따라 점수화한다",
                                          "가장 적절한 장기 프로젝트를 선택하거나 보류한다",
                                          "필요하면 선택된 프로젝트에 맞는 도구 요청을 검토한다"
                                        ],
                                        "self_assessment": {
                                          "confidence": 0.88,
                                          "risk_level": "low",
                                          "notes": "자기 모델과 환경 모델 v1은 현재 상태
                                          }
                                        }
                                        """;
    public static void TempTest()
    {
      MemoryManager.SavePassiveMemory(TempEventJson);
    }
}
