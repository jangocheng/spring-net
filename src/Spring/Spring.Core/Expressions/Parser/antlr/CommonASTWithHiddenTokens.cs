using System;
using Spring.Expressions.Parser.antlr.collections;

namespace Spring.Expressions.Parser.antlr
{
    /*ANTLR Translator Generator
    * Project led by Terence Parr at http://www.jGuru.com
    * Software rights: http://www.antlr.org/license.html
    *
    * $Id:$
    */

    //
    // ANTLR C# Code Generator by Micheal Jordan
    //                            Kunle Odutola       : kunle UNDERSCORE odutola AT hotmail DOT com
    //                            Anthony Oguntimehin
    //
    // With many thanks to Eric V. Smith from the ANTLR list.
    //

    /*A CommonAST whose initialization copies hidden token
    *  information from the Token used to create a node.
    */

    public class CommonASTWithHiddenTokens : CommonAST
    {
        public new static readonly CommonASTWithHiddenTokensCreator Creator = new CommonASTWithHiddenTokensCreator();

        protected internal IHiddenStreamToken hiddenBefore, hiddenAfter; // references to hidden tokens

        public CommonASTWithHiddenTokens()
        {
        }

        public CommonASTWithHiddenTokens(IToken tok) : base(tok)
        {
        }

        [Obsolete("Deprecated since version 2.7.2. Use ASTFactory.dup() instead.", false)]
        protected CommonASTWithHiddenTokens(CommonASTWithHiddenTokens another) : base(another)
        {
            hiddenBefore = another.hiddenBefore;
            hiddenAfter = another.hiddenAfter;
        }

        public virtual IHiddenStreamToken getHiddenAfter()
        {
            return hiddenAfter;
        }

        public virtual IHiddenStreamToken getHiddenBefore()
        {
            return hiddenBefore;
        }

        public override void initialize(AST t)
        {
            hiddenBefore = ((CommonASTWithHiddenTokens) t).getHiddenBefore();
            hiddenAfter = ((CommonASTWithHiddenTokens) t).getHiddenAfter();
            base.initialize(t);
        }

        public override void initialize(IToken tok)
        {
            IHiddenStreamToken t = (IHiddenStreamToken) tok;
            base.initialize(t);
            hiddenBefore = t.getHiddenBefore();
            hiddenAfter = t.getHiddenAfter();
        }

        #region Implementation of ICloneable

        [Obsolete("Deprecated since version 2.7.2. Use ASTFactory.dup() instead.", false)]
        public override object Clone()
        {
            return new CommonASTWithHiddenTokens(this);
        }

        #endregion

        public class CommonASTWithHiddenTokensCreator : ASTNodeCreator
        {
            /// <summary>
            ///     Returns the fully qualified name of the AST type that this
            ///     class creates.
            /// </summary>
            public override string ASTNodeTypeName
            {
                get
                {
                    return typeof(CommonASTWithHiddenTokens).FullName;
                    ;
                }
            }

            /// <summary>
            ///     Constructs a <see cref="AST" /> instance.
            /// </summary>
            public override AST Create()
            {
                return new CommonASTWithHiddenTokens();
            }
        }
    }
}