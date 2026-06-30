using System.Text.Json;
using FreeModel.Dto.ToolOutput;
using FreeModel.Enum;

namespace FreeModel.Service;

public static class MemoryMenagerHelper
{
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
    
    private static string Content = """
                                    
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
}
